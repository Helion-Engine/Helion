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
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.Builder
{
    /// <summary>
    /// The BSP tree builder that manages building the entire tree from some
    /// level.
    /// </summary>
    public abstract class BspBuilderBase : IBspBuilder
    {
        /// <summary>
        /// A counter which is used to make sure that we don't enter some 
        /// infinite loop due to any bugs. In a properly implemented builder
        /// this will never be reached.
        /// </summary>
        protected const int RecursiveOverflowAmount = 10000;

        /// <summary>
        /// The config file with BSP building information.
        /// </summary>
        protected BspConfig Config;

        /// <summary>
        /// A list of all the lines that map onto sectors.
        /// </summary>
        protected IList<SectorLine> lineIdToSector = new List<SectorLine>();

        /// <summary>
        /// All the work items when building a tree.
        /// </summary>
        protected Stack<BspWorkItem> WorkItems = new Stack<BspWorkItem>();

        /// <summary>
        /// A stack of nodes that parallels the work items stack.
        /// </summary>
        protected Stack<BspNode> NodeStack = new Stack<BspNode>();

        /// <summary>
        /// The root of the tree.
        /// </summary>
        protected BspNode Root = new BspNode();

        /// <summary>
        /// The current state of the builder.
        /// </summary>
        public BuilderStates States = new BuilderStates();

        /// <summary>
        /// The allocator for all vertices in the map.
        /// </summary>
        public VertexAllocator VertexAllocator;

        /// <summary>
        /// The segment allocator for all segments and splits in the map.
        /// </summary>
        public SegmentAllocator SegmentAllocator;

        /// <summary>
        /// Manages the convexity checking states of BSP building.
        /// </summary>
        public ConvexChecker ConvexChecker = new ConvexChecker();

        /// <summary>
        /// The calculator for split scores for a set of segments.
        /// </summary>
        public SplitCalculator SplitCalculator;

        /// <summary>
        /// A line partitioning helper after a splitter is chosen.
        /// </summary>
        public Partitioner Partitioner;

        /// <summary>
        /// Classifies junctions, which tell us whether we are inside the map
        /// or outside of it relative to some point (when making minisegs).
        /// </summary>
        public JunctionClassifier JunctionClassifier = new JunctionClassifier();

        /// <summary>
        /// The creator of minisegs, which are segments along a splitter that
        /// are not segments originally part of the level.
        /// </summary>
        public MinisegCreator MinisegCreator;

        /// <summary>
        /// True if building is done and the tree can be extracted/read, false
        /// if building still needs to be done.
        /// </summary>
        public bool Done => States.Current == BuilderState.Complete;

        protected BspBuilderBase(Map map) : this(map, new BspConfig())
        {
        }

        protected BspBuilderBase(Map map, BspConfig config)
        {
            Config = config;
            VertexAllocator = new VertexAllocator(config.VertexWeldingEpsilon);
            SegmentAllocator = new SegmentAllocator(VertexAllocator);
            SplitCalculator = new SplitCalculator(config);
            Partitioner = new Partitioner(config, SegmentAllocator, JunctionClassifier);
            MinisegCreator = new MinisegCreator(VertexAllocator, SegmentAllocator, JunctionClassifier);

            PopulateAllocatorsFrom(map);
            WorkItems.Push(new BspWorkItem(SegmentAllocator.ToList()));
            NodeStack.Push(Root);
        }

        /// <summary>
        /// Goes through all the vertices/line segments in the map provided and
        /// populates the vertex/segment allocators with the appropriate data.
        /// </summary>
        /// <param name="map">The map to get the components from.</param>
        protected void PopulateAllocatorsFrom(Map map)
        {
            map.Lines.ForEach(line =>
            {
                VertexIndex start = VertexAllocator[line.StartVertex.Position];
                VertexIndex end = VertexAllocator[line.EndVertex.Position];
                int frontSectorIndex = line.Front.Sector.Id;
                int backSectorIndex = line.Back?.Sector.Id ?? SectorLine.NoLineToSectorId;
                BspSegment bspSegment = SegmentAllocator.GetOrCreate(start, end, frontSectorIndex, backSectorIndex, line.Id);

                if (line.OneSided)
                    JunctionClassifier.AddOneSidedSegment(bspSegment);
                lineIdToSector.Add(new SectorLine(line.Segment.Delta, frontSectorIndex, backSectorIndex));
            });

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

        /// <summary>
        /// Takes the convex traversal that was done and adds it to the top BSP 
        /// node on the stack. This effectively creates the subsector.
        /// </summary>
        protected void AddConvexTraversalToTopNode()
        {
            ConvexTraversal traversal = ConvexChecker.States.ConvexTraversal;
            Rotation rotation = ConvexChecker.States.Rotation;
            IList<SubsectorEdge> edges = SubsectorEdge.FromClockwiseTraversal(traversal, lineIdToSector, rotation);
            NodeStack.Peek().ClockwiseEdges = edges;
        }

        /// <summary>
        /// Loads the next work item, which starts a full 'cycle' (from 
        /// convex checking to leaf node generation or miniseg splitting).
        /// </summary>
        protected void LoadNextWorkItem()
        {
            Invariant(WorkItems.Count > 0, "Expected a root work item to be present");

            ConvexChecker.Load(WorkItems.Peek().Segments);
            States.SetState(BuilderState.CheckingConvexity);
        }

        /// <summary>
        /// Performs the convexity check and populates the appropriate convex 
        /// checker states.
        /// </summary>
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

        /// <summary>
        /// Performs leaf node creation and sets the state to either convex
        /// checking or complete.
        /// </summary>
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
                LoadNextWorkItem();
        }

        /// <summary>
        /// Performs splitter finding.
        /// </summary>
        protected void ExecuteSplitterFinding()
        {
            switch (SplitCalculator.States.State)
            {
            case SplitterState.Loaded:
            case SplitterState.Working:
                SplitCalculator.Execute();
                break;

            case SplitterState.Finished:
                Partitioner.Load(SplitCalculator.States.BestSplitter, WorkItems.Peek().Segments);
                States.SetState(BuilderState.PartitioningSegments);
                break;
            }
        }

        /// <summary>
        /// Takes the splitter found in the previous step and performs segment
        /// partitioning.
        /// </summary>
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

        /// <summary>
        /// Generates minisegs along the partitioning line inside the level
        /// where no segments are.
        /// </summary>
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

        /// <summary>
        /// Finishes up the splitting.
        /// </summary>
        protected void ExecuteSplitFinalization()
        {
            BspWorkItem currentWorkItem = WorkItems.Pop();
            string path = currentWorkItem.BranchPath;

            BspNode parentNode = NodeStack.Pop();
            BspNode leftChild = new BspNode();
            BspNode rightChild = new BspNode();
            parentNode.SetChildren(leftChild, rightChild);
            parentNode.Splitter = SplitCalculator.States.BestSplitter;

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

            LoadNextWorkItem();
        }

        /// <summary>
        /// Performs an split-wise step.
        /// </summary>
        protected void Execute()
        {
            switch (States.Current)
            {
            case BuilderState.NotStarted:
                LoadNextWorkItem();
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

        public abstract BspNode? Build();
    }
}