using Helion.Bsp.Impl.Debuggable.Convex;
using Helion.Bsp.Impl.Debuggable.Miniseg;
using Helion.Bsp.Impl.Debuggable.Partition;
using Helion.Bsp.Impl.Debuggable.Split;
using Helion.Bsp.Node;
using Helion.Maps;

namespace Helion.Bsp.Impl.Debuggable
{
    public class DebuggableBspBuilder : BspBuilder
    {
        public readonly DebuggableConvexChecker ConvexChecker = new DebuggableConvexChecker();
        public readonly DebuggableSplitCalculator SplitCalculator = new DebuggableSplitCalculator();
        public readonly DebuggablePartitioner Partitioner = new DebuggablePartitioner();
        public readonly DebuggableMinisegCreator MinisegCreator = new DebuggableMinisegCreator();
        
        public DebuggableBspBuilder(BspConfig config, IMap map) : base(config, map)
        {
        }

        public override BspNode? Build()
        {
            // TODO
            return null;
        }
    }
}