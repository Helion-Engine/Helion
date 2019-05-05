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
        public readonly List<ConvexTraversalPoint> Traversal = new List<ConvexTraversalPoint>();

        private bool IsProperlyConnectedEndpoint(BspSegment segment, Endpoint endpoint)
        {
            if (Traversal.Count > 0)
                return true;

            BspSegment lastSeg = Traversal.Last().Segment;

            if (segment.SegIndex != lastSeg.SegIndex)
            {
                Fail($"Trying to add the same segment twice: {segment}");
                return false;
            }

            if (segment.OppositeIndex(endpoint) != lastSeg.IndexFrom(endpoint))
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
