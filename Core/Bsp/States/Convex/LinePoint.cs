using Helion.Bsp.Geometry;
using Helion.Util.Geometry;

namespace Helion.Bsp.States.Convex
{
    // TODO: Can we merge this with ConvexTraversalPoint? It's the same thing,
    // and may be a leftover from the C++ porting over.

    /// <summary>
    /// A helper class that contains a point in a traversal for a segment.
    /// </summary>
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
