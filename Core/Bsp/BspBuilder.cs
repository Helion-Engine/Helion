using System;
using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Bsp.Node;
using Helion.Bsp.States;
using Helion.Bsp.States.Convex;
using Helion.Bsp.States.Miniseg;
using Helion.Bsp.States.Partition;
using Helion.Bsp.States.Split;
using Helion.Maps;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Segments.Enums;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp
{
    public class BspBuilder
    {
        /// <summary>
        /// A counter which is used to make sure that we don't enter some 
        /// infinite loop due to any bugs. In a properly implemented builder
        /// this will never be reached.
        /// </summary>
        private const int RecursiveOverflowAmount = 10000;

        public BspState State { get; private set; } = BspState.NotStarted;
        public readonly ConvexChecker ConvexChecker;
        public readonly SplitCalculator SplitCalculator;
        public readonly Partitioner Partitioner;
        public readonly MinisegCreator MinisegCreator;
        public readonly VertexAllocator VertexAllocator;
        public readonly SegmentAllocator SegmentAllocator;
        protected readonly BspConfig BspConfig;
        protected readonly CollinearTracker CollinearTracker;
        protected readonly JunctionClassifier JunctionClassifier;
        protected readonly BspNode Root = new BspNode();
        protected readonly Stack<WorkItem> WorkItems = new Stack<WorkItem>();

        public bool Done => State == BspState.Complete;
        public WorkItem? CurrentWorkItem => WorkItems.TryPeek(out WorkItem? result) ? result : null;

        public BspBuilder(IMap map) : this(new BspConfig(), map)
        {
        }

        public BspBuilder(BspConfig config, IMap map)
        {
            BspConfig = config;
            VertexAllocator = new VertexAllocator(config.VertexWeldingEpsilon);
            CollinearTracker = new CollinearTracker(config.VertexWeldingEpsilon);
            SegmentAllocator = new SegmentAllocator(VertexAllocator, CollinearTracker);
            JunctionClassifier = new JunctionClassifier();
            ConvexChecker = new ConvexChecker();
            SplitCalculator = new SplitCalculator(config, CollinearTracker);
            Partitioner = new Partitioner(config, SegmentAllocator, JunctionClassifier);
            MinisegCreator = new MinisegCreator(VertexAllocator, SegmentAllocator, JunctionClassifier);

            List<BspSegment> segments = ReadMapLines(map);
            JunctionClassifier.Add(segments);
            
            WorkItems.Push(new WorkItem(Root, segments));
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

            // TODO: Only do this if we actually have degenerate nodes!
            Root.StripDegenerateNodes();
            return Root.IsDegenerate ? null : Root;
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

        private List<BspSegment> ReadMapLines(IMap map)
        {
            List<BspSegment> segments = new List<BspSegment>();
            foreach (IBspUsableLine line in map.GetLines())
            {
                int startIndex = VertexAllocator[line.StartPosition];
                int endIndex = VertexAllocator[line.EndPosition];
                BspSegment segment = SegmentAllocator.GetOrCreate(startIndex, endIndex, line);
                segments.Add(segment);
            }
            
            return SegmentChainPruner.Prune(segments);
        }

        /// <summary>
        /// Takes the convex traversal that was done and adds it to the top BSP 
        /// node on the stack. This effectively creates the subsector.
        /// </summary>
        private void AddConvexTraversalToTopNode()
        {
            Invariant(!WorkItems.Empty(), "Cannot add convex traversal to an empty work item stack");

            ConvexTraversal traversal = ConvexChecker.States.ConvexTraversal;
            Rotation rotation = ConvexChecker.States.Rotation;
            List<SubsectorEdge> edges = SubsectorEdge.FromClockwiseTraversal(traversal, rotation);

            WorkItems.Peek().Node.ClockwiseEdges = edges;
        }

        private void LoadNextWorkItem()
        {
            Invariant(WorkItems.Count > 0, "Expected a root work item to be present");

            ConvexChecker.Load(WorkItems.Peek().Segments);
            State = BspState.CheckingConvexity;
        }

        private void ExecuteConvexityCheck()
        {
            Invariant(WorkItems.Count < RecursiveOverflowAmount, "BSP recursive overflow detected");

            switch (ConvexChecker.States.State)
            {
            case ConvexState.Loaded:
            case ConvexState.Traversing:
                ConvexChecker.Execute();
                break;

            case ConvexState.FinishedIsDegenerate:
            case ConvexState.FinishedIsConvex:
                State = BspState.CreatingLeafNode;
                break;

            case ConvexState.FinishedIsSplittable:
                SplitCalculator.Load(WorkItems.Peek().Segments);
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

            WorkItems.Pop();

            if (WorkItems.Empty())
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
                Partitioner.Load(SplitCalculator.States.BestSplitter, WorkItems.Peek().Segments);
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
            WorkItem currentWorkItem = WorkItems.Pop();

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
                WorkItems.Push(new WorkItem(rightChild, rightSegs, path + "R"));
                WorkItems.Push(new WorkItem(leftChild, leftSegs, path + "L"));
            }
            else
            {
                WorkItems.Push(new WorkItem(leftChild, leftSegs, path + "L"));
                WorkItems.Push(new WorkItem(rightChild, rightSegs, path + "R"));
            }

            LoadNextWorkItem();
        }
    }
}