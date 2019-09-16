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

namespace Helion.Bsp.Impl.Debuggable
{
    public class DebuggableBspBuilder : BspBuilder
    {
        /// <summary>
        /// A counter which is used to make sure that we don't enter some 
        /// infinite loop due to any bugs. In a properly implemented builder
        /// this will never be reached.
        /// </summary>
        private const int RecursiveOverflowAmount = 10000;
        
        public DebuggableBspState State { get; private set; } = DebuggableBspState.NotStarted;
        public readonly DebuggableConvexChecker ConvexChecker;
        public readonly DebuggableSplitCalculator SplitCalculator;
        public readonly DebuggablePartitioner Partitioner;
        public readonly DebuggableMinisegCreator MinisegCreator;

        public bool Done => State == DebuggableBspState.Complete;
        public WorkItem? CurrentWorkItem => WorkItems.TryPeek(out WorkItem? result) ? result : null;

        public DebuggableBspBuilder(IMap map) : this(new BspConfig(), map)
        {
        }
        
        public DebuggableBspBuilder(BspConfig config, IMap map) : base(config, map)
        {
            ConvexChecker = new DebuggableConvexChecker();
            SplitCalculator = new DebuggableSplitCalculator(config, CollinearTracker);
            Partitioner = new DebuggablePartitioner(config, SegmentAllocator, JunctionClassifier);
            MinisegCreator = new DebuggableMinisegCreator(VertexAllocator, SegmentAllocator, JunctionClassifier);
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
            while (State != DebuggableBspState.CheckingConvexity && !Done);
        }

        /// <summary>
        /// Advances to the next major state.
        /// </summary>
        public void ExecuteMajorStep()
        {
            if (Done)
                return;

            DebuggableBspState originalState = State;
            DebuggableBspState currentState = State;
            while (originalState == currentState && !Done)
            {
                Execute();
                currentState = State;
            }
        }

        public VertexAllocator GetVertexAllocator() => VertexAllocator;
        public SegmentAllocator GetSegmentAllocator() => SegmentAllocator;
        
        public override BspNode? Build()
        {
            while (!Done)
                ExecuteFullCycleStep();
            
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
            case DebuggableBspState.NotStarted:
                LoadNextWorkItem();
                break;

            case DebuggableBspState.CheckingConvexity:
                ExecuteConvexityCheck();
                break;

            case DebuggableBspState.CreatingLeafNode:
                ExecuteLeafNodeCreation();
                break;

            case DebuggableBspState.FindingSplitter:
                ExecuteSplitterFinding();
                break;

            case DebuggableBspState.PartitioningSegments:
                ExecuteSegmentPartitioning();
                break;

            case DebuggableBspState.GeneratingMinisegs:
                ExecuteMinisegGeneration();
                break;

            case DebuggableBspState.FinishingSplit:
                ExecuteSplitFinalization();
                break;
            }
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
            State = DebuggableBspState.CheckingConvexity;
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
                State = DebuggableBspState.CreatingLeafNode;
                break;

            case ConvexState.FinishedIsSplittable:
                SplitCalculator.Load(WorkItems.Peek().Segments);
                State = DebuggableBspState.FindingSplitter;
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
                State = DebuggableBspState.Complete;
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
                State = DebuggableBspState.PartitioningSegments;
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
                State = DebuggableBspState.GeneratingMinisegs;
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
                State = DebuggableBspState.FinishingSplit;
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