using Helion.Bsp.Node;
using Helion.Maps;

namespace Helion.Bsp.Impl.Debuggable
{
    public class DebuggableBspBuilder : BspBuilder
    {
        public readonly DebuggableConvexChecker ConvexChecker;
        public readonly DebuggableSplitCalculator SplitCalculator;
        public readonly DebuggablePartitioner Partitioner;
        public readonly DebuggableMinisegCreator MinisegCreator;

        public DebuggableBspBuilder(IMap map) : this(new BspConfig(), map)
        {
        }
        
        public DebuggableBspBuilder(BspConfig config, IMap map) : base(config, map)
        {
            ConvexChecker = new DebuggableConvexChecker();
            SplitCalculator = new DebuggableSplitCalculator(config);
            Partitioner = new DebuggablePartitioner(config, SegmentAllocator, JunctionClassifier);
            MinisegCreator = new DebuggableMinisegCreator(VertexAllocator, SegmentAllocator, JunctionClassifier);
        }

        public override BspNode? Build()
        {
            // TODO
            return null;
        }
    }
}