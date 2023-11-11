using System.Collections.Generic;
using System.Linq;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Maps.Bsp.Geometry;
using Helion.Maps.Bsp.States.Convex;
using static Helion.Util.Assertion.Assert;

namespace Helion.Maps.Bsp.Node;

/// <summary>
/// An edge of a subsector, or better put: a segment on the edge of a
/// convex polygon that is the leaf of a BSP tree.
/// </summary>
public class SubsectorEdge
{
    public readonly IBspUsableLine? Line;
    public readonly bool IsFront;
    public Vec2D Start;
    public Vec2D End;
    public int? PartnerSegId;
    
    public bool IsMiniseg => Line == null;

    /// <summary>
    /// Creates a subsector edge from some geometric data and for some
    /// side.
    /// </summary>
    /// <param name="start">The starting point.</param>
    /// <param name="end">The ending point.</param>
    /// <param name="line">The line this is on top of, or null if this is
    /// a miniseg.</param>
    /// <param name="front">True if this is on the front side, false if it
    /// is the back. This value is not used if this is a miniseg. This
    /// must never be false for a one sided line.</param>
    public SubsectorEdge(Vec2D start, Vec2D end, IBspUsableLine? line, bool front, int? partnerSegId)
    {
        Start = start;
        End = end;
        IsFront = front;
        Line = line;
        PartnerSegId = partnerSegId;
    }

    /// <summary>
    /// Using the convex traversal and the rotation of the traversal, this
    /// will create a clockwise traversal to form the final subsector for
    /// a BSP node.
    /// </summary>
    /// <param name="convexTraversal">The traversal we did earlier that
    /// resulted in a convex subsector.</param>
    /// <param name="rotation">What direction that traversal went.</param>
    /// <returns>A convex series of sequential subsector edges that make up
    /// the closed subsector.</returns>
    public static List<SubsectorEdge> FromClockwiseTraversal(ConvexTraversal convexTraversal, Rotation rotation)
    {
        List<SubsectorEdge> edges = CreateSubsectorEdges(convexTraversal, rotation);
        if (rotation == Rotation.Left)
        {
            edges.ForEach(edge => edge.Reverse());
            edges.Reverse();
        }

        // TODO: Assert valid subsector edges!
        return edges;
    }

    private static List<SubsectorEdge> CreateSubsectorEdges(ConvexTraversal convexTraversal, Rotation rotation)
    {
        List<ConvexTraversalPoint> traversal = convexTraversal.Traversal;
        Precondition(traversal.Count >= 3, "Traversal must yield at least a triangle in size");

        List<SubsectorEdge> subsectorEdges = new();

        ConvexTraversalPoint firstTraversal = traversal.First();
        BspVertex startPoint = firstTraversal.Vertex;
        foreach (ConvexTraversalPoint traversalPoint in traversal)
        {
            BspSegment segment = traversalPoint.Segment;
            BspVertex endingPoint = segment.Opposite(traversalPoint.Endpoint);
            bool traversedFrontSide = CheckIfTraversedFrontSide(traversalPoint, rotation);

            SubsectorEdge edge = new(startPoint.Struct, endingPoint.Struct, segment.Line, traversedFrontSide, null);
            subsectorEdges.Add(edge);

            Invariant(startPoint != endingPoint, "Traversal produced the wrong endpoint indices");
            startPoint = endingPoint;
        }

        Postcondition(subsectorEdges.Count == traversal.Count, "Added too many subsector edges in traversal");
        return subsectorEdges;
    }

    private static bool CheckIfTraversedFrontSide(ConvexTraversalPoint traversalPoint, Rotation rotation)
    {
        switch (rotation)
        {
        case Rotation.Left:
            return traversalPoint.Endpoint == Endpoint.End;
        case Rotation.Right:
            return traversalPoint.Endpoint == Endpoint.Start;
        default:
            Fail("Should never be handling a non-rotational traversal");
            return true;
        }
    }

    private void Reverse()
    {
        Vec2D temp = Start;
        Start = End;
        End = temp;
    }
}
