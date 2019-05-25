using Helion.BSP.Geometry;
using Helion.BSP.Node;
using Helion.BSP.States;
using Helion.BSP.States.Convex;
using Helion.BSP.States.Miniseg;
using Helion.BSP.States.Partition;
using Helion.BSP.States.Split;
using Helion.Map;
using Helion.Util;
using System;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.BSP
{
    public abstract class BspBuilder
    {
        protected const int RecursiveOverflowAmount = 10000;

        protected BspConfig Config;
        protected IList<SectorLine> lineIdToSector = new List<SectorLine>();
        protected Stack<BspWorkItem> WorkItems = new Stack<BspWorkItem>();
        protected Stack<BspNode> NodeStack = new Stack<BspNode>();
        protected BspNode Root = new BspNode();
        public BuilderStates States = new BuilderStates();
        public VertexAllocator VertexAllocator;
        public SegmentAllocator SegmentAllocator;
        public ConvexChecker ConvexChecker = new ConvexChecker();
        public SplitCalculator SplitCalculator;
        public Partitioner Partitioner;
        public JunctionClassifier JunctionClassifier = new JunctionClassifier();
        public MinisegCreator MinisegCreator;

        public bool Done => States.Current == BuilderState.Complete;

        protected BspBuilder(ValidMapEntryCollection map) : this(map, new BspConfig())
        {
        }

        protected BspBuilder(ValidMapEntryCollection map, BspConfig config)
        {
            Config = config;
            VertexAllocator = new VertexAllocator(config.VertexWeldingEpsilon);
            SegmentAllocator = new SegmentAllocator(VertexAllocator);
            SplitCalculator = new SplitCalculator(config);
            Partitioner = new Partitioner(config, VertexAllocator, SegmentAllocator, JunctionClassifier);
            MinisegCreator = new MinisegCreator(VertexAllocator, SegmentAllocator, JunctionClassifier);

            PopulateAllocatorsFrom(map);
            WorkItems.Push(new BspWorkItem(SegmentAllocator.ToList()));
            NodeStack.Push(Root);
        }

        protected void PopulateAllocatorsFrom(ValidMapEntryCollection map)
        {
            int lineId = 0;
            foreach (MapSegment seg in MapSegmentGenerator.Generate(map))
            {
                VertexIndex start = VertexAllocator[seg.Start];
                VertexIndex end = VertexAllocator[seg.End];
                int backSectorIndex = seg.BackSectorIndex ?? SectorLine.NoLineToSectorId;
                BspSegment bspSegment = SegmentAllocator.GetOrCreate(start, end, seg.FrontSectorIndex, backSectorIndex, lineId);
                lineId++;

                if (seg.OneSided)
                    JunctionClassifier.AddOneSidedSegment(bspSegment);
                lineIdToSector.Add(new SectorLine(seg.Delta, seg.FrontSectorIndex, backSectorIndex));
            }

            // We wait until we added every seg so that junction creation can
            // be done with knowledge of all the lines that exist. The junction
            // is defined to be the closest angle between two one-sided lines.
            // If we add all the lines one by one, we have no idea if we should
            // make a junction or not because a line we add later could be a
            // closer angle (since three or more lines can come out of a single
            // vertex).
            //
            // This forces our hand to wait until the end so we have all of the
            // information. Otherwise if we implement it such that it will auto
            // update for every new segment we add, we'll take a performance
            // hit and add a lot more code because we will need to re-evaluate
            // every single line. For a very degenerate map, this could even be
            // O(n^2). I am however not opposed to doing this if someone can 
            // find a clean way to do it and show the performance hit is okay.
            JunctionClassifier.NotifyDoneAddingOneSidedSegments();
        }

        protected void AddConvexTraversalToTopNode()
        {
            ConvexTraversal traversal = ConvexChecker.States.ConvexTraversal;
            IList<SubsectorEdge> edges = SubsectorEdge.FromClockwiseConvexTraversal(traversal, lineIdToSector);
            NodeStack.Peek().ClockwiseEdges = edges;
        }

        protected void StartBuilding()
        {
            Invariant(WorkItems.Count > 0, "Expected a root work item to be present");

            ConvexChecker.Load(WorkItems.Peek().Segments);
            States.SetState(BuilderState.CheckingConvexity);
        }

        protected void ExecuteConvexityCheck()
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
                States.SetState(BuilderState.CreatingLeafNode);
                break;

            case ConvexState.FinishedIsSplittable:
                SplitCalculator.Load(WorkItems.Peek().Segments);
                States.SetState(BuilderState.FindingSplitter);
                break;
            }
        }

        protected void ExecuteLeafNodeCreation()
        {
            ConvexState convexState = ConvexChecker.States.State;
            Invariant(convexState == ConvexState.FinishedIsDegenerate || convexState == ConvexState.FinishedIsConvex, "Unexpected BSP leaf building state");

            if (convexState == ConvexState.FinishedIsConvex)
                AddConvexTraversalToTopNode();

            WorkItems.Pop();
            NodeStack.Pop();

            if (WorkItems.Count == 0)
            {
                Root.StripDegenerateNodes();
                States.SetState(BuilderState.Complete);
            }
            else
                States.SetState(BuilderState.CheckingConvexity);
        }

        protected void ExecuteSplitterFinding()
        {
            switch (SplitCalculator.States.State)
            {
            case SplitterState.Loaded:
            case SplitterState.Working:
                SplitCalculator.Execute();
                break;

            case SplitterState.Finished:
                if (SplitCalculator.States.BestSplitter == null)
                    throw new HelionException("Invalid split calculator state");
                BspSegment splitter = SplitCalculator.States.BestSplitter;
                Partitioner.Load(splitter, WorkItems.Peek().Segments);
                States.SetState(BuilderState.PartitioningSegments);
                break;
            }
        }

        protected void ExecuteSegmentPartitioning()
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
                States.SetState(BuilderState.GeneratingMinisegs);
                break;
            }
        }

        protected void ExecuteMinisegGeneration()
        {
            switch (MinisegCreator.States.State)
            {
            case MinisegState.Loaded:
            case MinisegState.Working:
                MinisegCreator.Execute();
                break;

            case MinisegState.Finished:
                States.SetState(BuilderState.FinishingSplit);
                break;
            }
        }

        protected void ExecuteSplitFinalization()
        {
            string path = WorkItems.Peek().BranchPath;

            BspNode parentNode = NodeStack.Pop();
            BspNode leftChild = new BspNode();
            BspNode rightChild = new BspNode();
            parentNode.SetChildren(leftChild, rightChild);
            parentNode.Splitter = SplitCalculator.States.BestSplitter;

            WorkItems.Pop();

            // We arbitrarily decide to build left first, so left is stacked after.
            IList<BspSegment> right = Partitioner.States.RightSegments;
            IList<BspSegment> left = Partitioner.States.LeftSegments;
            foreach (BspSegment segment in MinisegCreator.States.Minisegs)
            {
                right.Add(segment);
                left.Add(segment);
            }

            WorkItems.Push(new BspWorkItem(right, path + "R"));
            WorkItems.Push(new BspWorkItem(left, path + "L"));
            NodeStack.Push(rightChild);
            NodeStack.Push(leftChild);

            States.SetState(BuilderState.CheckingConvexity);
        }

        protected void Execute()
        {
            switch (States.Current)
            {
            case BuilderState.NotStarted:
                StartBuilding();
                break;

            case BuilderState.CheckingConvexity:
                ExecuteConvexityCheck();
                break;

            case BuilderState.CreatingLeafNode:
                ExecuteLeafNodeCreation();
                break;

            case BuilderState.FindingSplitter:
                ExecuteSplitterFinding();
                break;

            case BuilderState.PartitioningSegments:
                ExecuteSegmentPartitioning();
                break;

            case BuilderState.GeneratingMinisegs:
                ExecuteMinisegGeneration();
                break;

            case BuilderState.FinishingSplit:
                ExecuteSplitFinalization();
                break;

            case BuilderState.Complete:
            default:
                break;
            }
        }
    }
}
