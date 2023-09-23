using System.Collections.Generic;
using System.Linq;
using Helion.Geometry.Segments;
using Helion.Maps.Bsp.Geometry;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Maps.Bsp.States.Convex;

/// <summary>
/// The information for a convex traversal of a set of connected lines.
/// </summary>
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

    /// <summary>
    /// Adds a new segment to the traversal. It is assumed that this is a
    /// valid addition.
    /// </summary>
    /// <param name="segment">The segment visited.</param>
    /// <param name="endpoint">The first vertex endpoint that was visited
    /// when traversing the convex subsector.</param>
    public void AddTraversal(BspSegment segment, Endpoint endpoint)
    {
        Precondition(IsProperlyConnectedEndpoint(segment, endpoint), "Provided a disconnected segment");

        Traversal.Add(new ConvexTraversalPoint(segment, endpoint));
    }

    private bool IsProperlyConnectedEndpoint(BspSegment segment, Endpoint endpoint)
    {
        if (Traversal.Empty())
            return true;

        ConvexTraversalPoint lastPoint = Traversal.Last();
        BspSegment lastSeg = lastPoint.Segment;
        Endpoint lastEndpoint = lastPoint.Endpoint;

        if (ReferenceEquals(segment, lastSeg))
        {
            Fail("Trying to add the same segment twice");
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
            Fail("Expect a tail-to-head connection");
            return false;
        }

        return true;
    }
}
