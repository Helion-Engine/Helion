using Helion.BSP.Geometry;
using Helion.BSP.Node;
using Helion.BSP.States;
using Helion.BSP.States.Convex;
using Helion.BSP.States.Miniseg;
using Helion.BSP.States.Partition;
using Helion.BSP.States.Split;
using Helion.Map;
using System.Collections.Generic;

namespace Helion.BSP
{
    public abstract class BspBuilder
    {
        protected BspConfig Config;
        protected VertexAllocator VertexAllocator;
        protected SegmentAllocator SegmentAllocator;
        protected JunctionClassifier junctionClassifier;
        protected IList<SectorLine> lineIdToSector = new List<SectorLine>();
        protected Stack<BspWorkItem> WorkItems = new Stack<BspWorkItem>();
        protected Stack<BspNode> NodeStack = new Stack<BspNode>();
        protected BspNode Root = new BspNode();
        public BuilderStates States = new BuilderStates();
        public ConvexChecker ConvexChecker = new ConvexChecker();
        public SplitCalculator SplitCalculator;
        public Partitioner Partitioner;
        public MinisegCreator MinisegCreator;
        public JunctionClassifier JunctionClassifier = new JunctionClassifier();

        protected BspBuilder(ValidMapEntryCollection map) : this(map, new BspConfig())
        {
        }

        protected BspBuilder(ValidMapEntryCollection map, BspConfig config)
        {
            Config = config;
            VertexAllocator = new VertexAllocator(config.VertexWeldingEpsilon);
            SegmentAllocator = new SegmentAllocator(VertexAllocator);
            junctionClassifier = new JunctionClassifier();
            SplitCalculator = new SplitCalculator(config);
            Partitioner = new Partitioner(config, VertexAllocator, SegmentAllocator, JunctionClassifier);
        }

        protected void PopulateAllocatorsFrom(ValidMapEntryCollection map)
        {
            // TODO
        }

        protected void CreateRootWorkItem()
        {
            // TODO
        }

        protected void AddConvexTraversalToTopNode()
        {
            // TODO
        }

        protected void StartBuilding()
        {
            // TODO
        }

        protected void ExecuteConvexityCheck()
        {
            // TODO
        }

        protected void ExecuteLeafNodeCreation()
        {
            // TODO
        }

        protected void ExecuteSplitterFinding()
        {
            // TODO
        }

        protected void ExecuteSegmentPartitioning()
        {
            // TODO
        }

        protected void ExecuteMinisegGeneration()
        {
            // TODO
        }

        protected void ExecuteSplitFinalization()
        {
            // TODO
        }
    }
}
