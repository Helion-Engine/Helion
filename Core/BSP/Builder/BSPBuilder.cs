using Helion.BSP.Geometry;
using Helion.Map;

namespace Helion.BSP
{
    public abstract class BspBuilder
    {
        protected BspConfig Config;
        protected VertexAllocator VertexAllocator;
        protected SegmentAllocator SegmentAllocator;

        protected BspBuilder(ValidMapEntryCollection map) : this(map, new BspConfig())
        {
        }

        protected BspBuilder(ValidMapEntryCollection map, BspConfig config)
        {
            Config = config;
            VertexAllocator = new VertexAllocator(config.VertexWeldingEpsilon);
            SegmentAllocator = new SegmentAllocator(VertexAllocator);
        }
    }
}
