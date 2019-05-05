using Helion.BSP.Geometry;
using Helion.Util.Geometry;

namespace Helion.BSP.States.Convex
{
    public class LinePoint
    {
        public readonly BspSegment Segment;
        public readonly Endpoint Endpoint;

        public LinePoint(BspSegment segment, Endpoint endpoint)
        {
            Segment = segment;
            Endpoint = endpoint;
        }
    }
}
