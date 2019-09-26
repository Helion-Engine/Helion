using System;
using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Bsp.Node;
using Helion.Bsp.Repairer;
using Helion.Bsp.States;
using Helion.Bsp.States.Convex;
using Helion.Bsp.States.Miniseg;
using Helion.Bsp.States.Partition;
using Helion.Bsp.States.Split;
using Helion.Maps;
using Helion.Maps.Components;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Segments.Enums;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp
{
    /// <summary>
    /// Builds a BSP tree.
    /// </summary>
    /// <remarks>
    /// Currently not thread safe, some state is shared (the non-readonly ones)
    /// which prevents threading from being used recursively on a tree. However
    /// it is possible to apply threading to each stage itself since those are
    /// 'embarrassingly parallel'.
    /// </remarks>
    public class BspBuilder
    {
        /// <summary>
        /// A counter which is used to make sure that we don't enter some 
        /// infinite loop due to any bugs. In a properly implemented builder
        /// this will never be reached.
        /// </summary>
        private const int RecursiveOverflowAmount = 10000;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public BspState State { get; private set; } = BspState.NotStarted;
        public readonly ConvexChecker ConvexChecker;
        public readonly SplitCalculator SplitCalculator;
        public readonly Partitioner Partitioner;
        public readonly MinisegCreator MinisegCreator;
        public readonly VertexAllocator VertexAllocator;
        public readonly SegmentAllocator SegmentAllocator;
        private readonly BspConfig BspConfig;
        private readonly BspNode m_root = new BspNode();
        private readonly Stack<WorkItem> m_workItems = new Stack<WorkItem>();
        private bool m_foundDegenerateNode;

        public bool Done => State == BspState.Complete;
        public WorkItem? CurrentWorkItem => m_workItems.TryPeek(out WorkItem? result) ? result : null;

        public BspBuilder(IMap map) : this(new BspConfig(), map)
        {
        }

        public BspBuilder(BspConfig config, IMap map)
        {
            BspConfig = config;
            CollinearTracker collinearTracker = new CollinearTracker(config.VertexWeldingEpsilon);
            JunctionClassifier junctionClassifier = new JunctionClassifier();
            
            VertexAllocator = new VertexAllocator(config.VertexWeldingEpsilon);
            SegmentAllocator = new SegmentAllocator(VertexAllocator, collinearTracker);
            ConvexChecker = new ConvexChecker();
            SplitCalculator = new SplitCalculator(config, collinearTracker);
            Partitioner = new Partitioner(config, SegmentAllocator, junctionClassifier);
            MinisegCreator = new MinisegCreator(VertexAllocator, SegmentAllocator, junctionClassifier);

            List<BspSegment> segments = ProcessMapLines(map);

            junctionClassifier.Add(segments);
            CreateInitialWorkItem(segments);
        }

        /// <summary>
        /// Moves until either the current work item is the branch provided, or
        /// the building is done.
        /// </summary>
        /// <param name="branch">The branch path to go to.</param>
        public void ExecuteUntilBranch(string branch)
        {
            string upperBranch = branch.ToUpper();
            while (!Done)
            {
                WorkItem? item = CurrentWorkItem;
                if (item == null || item.BranchPath == upperBranch)
                    break;
                ExecuteFullCycleStep();
            }
        }

        /// <summary>
        /// Steps through all major states until it reaches the convexity check
        /// or it completes.
        /// </summary>
        public void ExecuteFullCycleStep()
        {
            if (Done)
                return;

            do
                ExecuteMajorStep();
            while (State != BspState.CheckingConvexity && !Done);
        }

        /// <summary>
        /// Advances to the next major state.
        /// </summary>
        public void ExecuteMajorStep()
        {
            if (Done)
                return;

            BspState originalState = State;
            BspState currentState = State;
            while (originalState == currentState && !Done)
            {
                Execute();
                currentState = State;
            }
        }

        public BspNode? Build()
        {
            while (!Done)
                ExecuteFullCycleStep();

            if (m_foundDegenerateNode)
                m_root.StripDegenerateNodes();
            
            return m_root.IsDegenerate ? null : m_root;
        }

        /// <summary>
        /// Executes an atomic step forward, meaning it moves ahead by the most
        /// indivisible element that allows a debugging session to see every
        /// state change independently.
        /// </summary>
        public void Execute()
        {
            switch (State)
            {
            case BspState.NotStarted:
                LoadNextWorkItem();
                break;

            case BspState.CheckingConvexity:
                ExecuteConvexityCheck();
                break;

            case BspState.CreatingLeafNode:
                ExecuteLeafNodeCreation();
                break;

            case BspState.FindingSplitter:
                ExecuteSplitterFinding();
                break;

            case BspState.PartitioningSegments:
                ExecuteSegmentPartitioning();
                break;

            case BspState.GeneratingMinisegs:
                ExecuteMinisegGeneration();
                break;

            case BspState.FinishingSplit:
                ExecuteSplitFinalization();
                break;
            }
        }

        private List<BspSegment> ProcessMapLines(IMap map)
        {
            List<BspSegment> segments = new List<BspSegment>();
            foreach (ILine line in map.GetLines())
            {
                BspVertex start = VertexAllocator[line.StartPosition];
                BspVertex end = VertexAllocator[line.EndPosition];
                BspSegment segment = SegmentAllocator.GetOrCreate(start, end, line);
                segments.Add(segment);
            }
            
            if (BspConfig.AttemptMapRepair)
                segments = MapRepairer.Repair(segments, VertexAllocator, SegmentAllocator);
            
            List<BspSegment> prunedSegments = SegmentChainPruner.Prune(segments);
            if (prunedSegments.Count < segments.Count)
                Log.Debug("Pruned {0} dangling segments", segments.Count - prunedSegments.Count);
            
            return prunedSegments;
        }

        private void CreateInitialWorkItem(List<BspSegment> segments)
        {
            WorkItem workItem = new WorkItem(m_root, segments);
            m_workItems.Push(workItem);
        }
        
        /// <summary>
        /// Takes the convex traversal that was done and adds it to the top BSP 
        /// node on the stack. This effectively creates the subsector.
        /// </summary>
        private void AddConvexTraversalToTopNode()
        {
            Invariant(!m_workItems.Empty(), "Cannot add convex traversal to an empty work item stack");

            ConvexTraversal traversal = ConvexChecker.States.ConvexTraversal;
            Rotation rotation = ConvexChecker.States.Rotation;
            List<SubsectorEdge> edges = SubsectorEdge.FromClockwiseTraversal(traversal, rotation);

            m_workItems.Peek().Node.ClockwiseEdges = edges;
        }

        private void LoadNextWorkItem()
        {
            Invariant(m_workItems.Count > 0, "Expected a root work item to be present");

            ConvexChecker.Load(m_workItems.Peek().Segments);
            State = BspState.CheckingConvexity;
        }

        private void ExecuteConvexityCheck()
        {
            Invariant(m_workItems.Count < RecursiveOverflowAmount, "BSP recursive overflow detected");

            switch (ConvexChecker.States.State)
            {
            case ConvexState.Loaded:
            case ConvexState.Traversing:
                ConvexChecker.Execute();
                break;

            case ConvexState.FinishedIsDegenerate:
                m_foundDegenerateNode = true;
                goto case ConvexState.FinishedIsConvex;
            case ConvexState.FinishedIsConvex:
                State = BspState.CreatingLeafNode;
                break;

            case ConvexState.FinishedIsSplittable:
                WorkItem workItem = m_workItems.Peek();
                SplitCalculator.Load(workItem.Segments);
                State = BspState.FindingSplitter;
                break;
            }
        }

        private void ExecuteLeafNodeCreation()
        {
            ConvexState convexState = ConvexChecker.States.State;
            Invariant(convexState == ConvexState.FinishedIsDegenerate || convexState == ConvexState.FinishedIsConvex, "Unexpected BSP leaf building state");

            if (convexState == ConvexState.FinishedIsConvex)
                AddConvexTraversalToTopNode();

            m_workItems.Pop();

            if (m_workItems.Empty())
                State = BspState.Complete;
            else
                LoadNextWorkItem();
        }

        private void ExecuteSplitterFinding()
        {
            switch (SplitCalculator.States.State)
            {
            case SplitterState.Loaded:
            case SplitterState.Working:
                SplitCalculator.Execute();
                break;

            case SplitterState.Finished:
                Partitioner.Load(SplitCalculator.States.BestSplitter, m_workItems.Peek().Segments);
                State = BspState.PartitioningSegments;
                break;
            }
        }

        private void ExecuteSegmentPartitioning()
        {
            switch (Partitioner.States.State)
            {
            case PartitionState.Loaded:
            case PartitionState.Working:
                Partitioner.Execute();
                break;

            case PartitionState.Finished:
                if (Partitioner.States.Splitter == null)
                    throw new NullReferenceException("Unexpected null partition splitter");
                BspSegment? splitter = Partitioner.States.Splitter;
                MinisegCreator.Load(splitter, Partitioner.States.CollinearVertices);
                State = BspState.GeneratingMinisegs;
                break;
            }
        }

        private void ExecuteMinisegGeneration()
        {
            switch (MinisegCreator.States.State)
            {
            case MinisegState.Loaded:
            case MinisegState.Working:
                MinisegCreator.Execute();
                break;

            case MinisegState.Finished:
                State = BspState.FinishingSplit;
                break;
            }
        }

        private void ExecuteSplitFinalization()
        {
            WorkItem currentWorkItem = m_workItems.Pop();

            BspNode parentNode = currentWorkItem.Node;
            BspNode leftChild = new BspNode();
            BspNode rightChild = new BspNode();
            parentNode.SetChildren(leftChild, rightChild);
            parentNode.Splitter = SplitCalculator.States.BestSplitter;

            List<BspSegment> rightSegs = Partitioner.States.RightSegments;
            List<BspSegment> leftSegs = Partitioner.States.LeftSegments;
            rightSegs.AddRange(MinisegCreator.States.Minisegs);
            leftSegs.AddRange(MinisegCreator.States.Minisegs);

            string path = currentWorkItem.BranchPath;

            if (BspConfig.BranchRight)
            {
                m_workItems.Push(new WorkItem(leftChild, leftSegs, path + "L"));
                m_workItems.Push(new WorkItem(rightChild, rightSegs, path + "R"));
            }
            else
            {
                m_workItems.Push(new WorkItem(rightChild, rightSegs, path + "R"));
                m_workItems.Push(new WorkItem(leftChild, leftSegs, path + "L"));
            }

            LoadNextWorkItem();
        }
    }
}