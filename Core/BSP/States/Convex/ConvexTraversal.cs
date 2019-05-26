using Helion.BSP.Geometry;
using Helion.Util.Geometry;
using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assert;

namespace Helion.BSP.States.Convex
{
    public class ConvexTraversalPoint
    {
        public readonly BspSegment Segment;
        public readonly Endpoint Endpoint;

        public ConvexTraversalPoint(BspSegment segment, Endpoint endpoint)
        {
            Segment = segment;
            Endpoint = endpoint;
        }

        public Vec2D ToPoint() => Segment[Endpoint];
    }

    public class ConvexTraversal
    {
        /// <summary>
        /// Contains a series of vertex iterations with the segment for each
        /// vertex.
        /// </summary>
        /// <remarks>
        /// The vertex maps onto the first vertex of the segment when doing a
        /// traversal. This means that if `Start` was the pivot, then it will
        /// have the vertex being `Start` for the segment. Likewise if we had
        /// reached this line through `End` first, then the segment would be
        /// indexed here with the `End` vertex.
        /// </remarks>
        public readonly List<ConvexTraversalPoint> Traversal = new List<ConvexTraversalPoint>();

        private bool IsProperlyConnectedEndpoint(BspSegment segment, Endpoint endpoint)
        {
            if (Traversal.Count == 0)
                return true;

            ConvexTraversalPoint lastPoint = Traversal.Last();
            BspSegment lastSeg = lastPoint.Segment;
            Endpoint lastEndpoint = lastPoint.Endpoint;

            if (segment.SegIndex == lastSeg.SegIndex)
            {
                Fail($"Trying to add the same segment twice: {segment}");
                return false;
            }

            // Because our traversal uses the first endpoint we reached, that
            // means the last segment's opposite endpoint should match this 
            // segment's current endpoint. Graphically, this means:
            //
            // LastEndpoint  Endpoint
            //     o-----------o------------o
            //         LastSeg    Segment
            //
            // Notice that the opposite endpoint of LastEndpoint is equal to 
            // the middle vertex labeled Endpoint. This is what we want to make
            // sure are equal since it is not a valid traversal if they do not
            // match.
            if (segment.IndexFrom(endpoint) != lastSeg.OppositeIndex(lastEndpoint))
            {
                Fail($"Expect a tail-to-head connection for: {lastSeg} -> {segment}");
                return false;
            }

            return true;
        }

        public void AddTraversal(BspSegment segment, Endpoint endpoint)
        {
            Precondition(IsProperlyConnectedEndpoint(segment, endpoint), "Provided a disconnected segment");
            Traversal.Add(new ConvexTraversalPoint(segment, endpoint));
        }
    }
}
