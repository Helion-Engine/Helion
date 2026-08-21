using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.Util.Container;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shared.World.ViewClipping;

// TODO: Implement this with some kind of discrete fused interval tree for O(lg n).

/// <summary>
/// A helper class that treats a 360 circle as an interval range. Allows
/// adding of ranges and querying whether some angle span is entire blocked
/// or not.
/// </summary>
/// <remarks>
/// The angles stored internally are 'diamond angles', which don't map on
/// exactly to real angles. The purpose of these angles is to allow for an
/// ordering of different sloped lines from some origin point. Before each
/// use of this class, it should be cleared, and have the center set to the
/// central view reference point.
/// </remarks>
public class ViewClipper
{
    private const uint DiamondScale = uint.MaxValue / 4;
    private const uint PiAngle = uint.MaxValue / 2;
    private const double RadiansToDiamondAngleFactor = uint.MaxValue / MathHelper.TwoPi;

    private DynamicArray<ClipSpan> m_spans = new DynamicArray<ClipSpan>(256);

    /// <summary>
    /// The center point from which we will clip from.
    /// </summary>
    public Vec2D Center = Vec2D.Zero;

    public IEnumerable<ClipSpan> Elements => m_spans.Data.Take(m_spans.Length);

    /// <summary>
    /// Takes two positions and finds the diamond angle that exists from
    /// start to end. This is also known as the vector angle, but the
    /// calculations find it with respect to being a diamond. The diamond
    /// angle is an ordered angle that is similar to degrees or radians,
    /// and has absolute ordering.
    /// </summary>
    /// <remarks>
    /// https://stackoverflow.com/questions/1427422/cheap-algorithm-to-find-measure-of-angle-between-vectors
    /// is where the optimization was learned from.
    /// </remarks>
    /// <param name="start">The origin.</param>
    /// <param name="end">The endpoint from the origin forming a vector.
    /// </param>
    /// <returns>The diamond angle for the vertex. This will be zero if the
    /// start and end vertices are the same.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToDiamondAngle(double startX, double startY, double endX, double endY)
    {
        // The code below takes some position and finds the vector from the
        // center to the position.
        //
        // It then is able to take the X and Y components of this vector,
        // and turn them into some ratio between [0.0, 4.0) as follows for
        // this table:
        //
        //      X,  Y    Result
        //     -----------------
        //      1,  0     0.0
        //      0,  1     1.0
        //     -1,  0     2.0
        //      0, -1     3.0
        //
        // As such, we can then multiply it by a big number to turn it into
        // a value between [0, 2^32). The key here is that we get an order
        // out of the values, because this allows us to see what angles are
        // blocked or not by mapping every position onto a unit circle with
        // 2^32 precision.
        var posX = endX - startX;
        var posY = endY - startY;
        if (posX == 0 && posY == 0)
            return 0;

        // TODO: Can we fuse two if statements into one statement somehow?
        if (posY >= 0)
        {
            if (posX >= 0)
                return (uint)(DiamondScale * (posY / (posX + posY)));
            return (uint)(DiamondScale * (1 - (posX / (-posX + posY))));
        }

        if (posX < 0)
            return (uint)(DiamondScale * (2 - (posY / (-posX - posY))));
        return (uint)(DiamondScale * (3 + (posX / (posX - posY))));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static uint DiamondAngleFromRadians(double radians)
    {
        unchecked
        {
            return (uint)((long)(radians * RadiansToDiamondAngleFactor) % uint.MaxValue);
        }
    }

    /// <summary>
    /// Clears all the clip ranges.
    /// </summary>
    /// <remarks>
    /// Unless you know what you are doing, you should also set the
    /// <see cref="Center"/> variable to a new position.
    /// </remarks>
    public void Clear()
    {
        m_spans.Clear();
    }

    /// <summary>
    /// Adds two positions that will be converted into angles and then
    /// added to be a clipping range.
    /// </summary>
    /// <param name="first">The first vertex of a line segment.</param>
    /// <param name="second">The second vertex of a line segment.</param>
    public void AddLine(in Vec2D first, in Vec2D second)
    {
        var smallerAngle = ToDiamondAngle(Center.X, Center.Y, first.X, first.Y);
        var largerAngle = ToDiamondAngle(Center.X, Center.Y, second.X, second.Y);

        if (largerAngle < smallerAngle)
            (smallerAngle, largerAngle) = (largerAngle, smallerAngle);

        if (AnglesSpanOriginVector(smallerAngle, largerAngle))
        {
            AddRange(0, smallerAngle);
            AddRange(largerAngle, uint.MaxValue);
        }
        else
            AddRange(smallerAngle, largerAngle);
    }

    public void AddLine(uint smallerAngle, uint largerAngle)
    {
        if (AnglesSpanOriginVector(smallerAngle, largerAngle))
        {
            AddRange(0, smallerAngle);
            AddRange(largerAngle, uint.MaxValue);
        }
        else
            AddRange(smallerAngle, largerAngle);
    }

    public bool InsideAnyRange(Vec2D start, Vec2D end) => InsideAnyRange(start.X, start.Y, end.X, end.Y);

    /// <summary>
    /// Checks if the two points provided are encased in any ranges.
    /// </summary>
    /// <param name="first">The first vertex of a line segment.</param>
    /// <param name="second">The second vertex of a line segment.</param>
    /// <returns>True if they are in a range, false if not.</returns>
    public bool InsideAnyRange(double x1, double y1, double x2, double y2)
    {
        if (m_spans.Length == 0)
            return false;

        var smallerAngle = ToDiamondAngle(Center.X, Center.Y, x1, y1);
        var largerAngle = ToDiamondAngle(Center.X, Center.Y, x2, y2);

        if (largerAngle < smallerAngle)
            (smallerAngle, largerAngle) = (largerAngle, smallerAngle);

        if (AnglesSpanOriginVector(smallerAngle, largerAngle))
            return InRange(0, smallerAngle) && InRange(largerAngle, uint.MaxValue);
        return InRange(smallerAngle, largerAngle);
    }

    public bool InsideAnyRange(uint smallerAngle, uint largerAngle)
    {
        if (AnglesSpanOriginVector(smallerAngle, largerAngle))
            return InRange(0, smallerAngle) && InRange(largerAngle, uint.MaxValue);
        return InRange(smallerAngle, largerAngle);
    }

    public (uint, uint) GetAngles(in Vec2D first, in Vec2D second)
    {
        var smallerAngle = ToDiamondAngle(Center.X, Center.Y, first.X, first.Y);
        var largerAngle = ToDiamondAngle(Center.X, Center.Y, second.X, second.Y);

        if (largerAngle < smallerAngle)
            (smallerAngle, largerAngle) = (largerAngle, smallerAngle);

        return (smallerAngle, largerAngle);
    }

    /// <summary>
    /// Checks if the arc from the chord crosses the origin vector [1, 0].
    /// </summary>
    /// <param name="smallerAngle">The starting angle.</param>
    /// <param name="largerAngle">the ending angle.</param>
    /// <returns>If the chord from the start and end angle crosses the
    /// initial vector angle of zero degrees.</returns>
    private static bool AnglesSpanOriginVector(uint smallerAngle, uint largerAngle)
    {
        Precondition(smallerAngle <= largerAngle, "Smaller angle should be smaller than the larger angle");

        // In this case, the only place the end point could be is in the
        // range of (start, MAX_ANGLE), since if it was beyond the maximum
        // angle then it'd be less than the smaller angle (which means this
        // wouldn't even be an issue). Also this lets us avoid doing any
        // unchecked calculations and avoid overflow for the next part.
        if (smallerAngle >= PiAngle)
            return false;

        // A proof can be done to demonstrate that the only time it is
        // shorter to go right is when the smaller angle is less than 180
        // degrees, and adding 180 to it is less than the larger angle.
        //
        // The abridged version is that if the gap between the start and
        // the end is larger than 180, then instead of going CCW around
        // the circle, you'd go CW since it's the shortest distance for
        // a chord formed by the two endpoints. This means if you are less
        // starting at the top half of the circle, you must go right, and
        // pass through the origin vector (aka: <1, 0>).
        return smallerAngle + PiAngle < largerAngle;
    }

    /// <summary>
    /// Checks if the start/end angles are contained in any interval.
    /// </summary>
    /// <param name="startAngle">The starting angle.</param>
    /// <param name="endAngle">the ending angle.</param>
    /// <returns>True if so, false otherwise.</returns>
    private bool InRange(uint startAngle, uint endAngle)
    {
        // TODO: If endAngle > uint.MaxValue / 2, search backwards?

        for (int i = 0; i < m_spans.Length; i++)
        {
            ref var span = ref m_spans.Data[i];
            if (span.Contains(startAngle, endAngle))
                return true;

            if (endAngle < span.StartAngle)
                return false;
        }

        return false;
    }

    /// <summary>
    /// Adds the range of endpoints inclusively, generating and fusing any
    /// nodes that overlap with the range.
    /// </summary>
    /// <param name="startAngle">The starting angle.</param>
    /// <param name="endAngle">The ending angle.</param>
    private void AddRange(uint startAngle, uint endAngle)
    {
        Precondition(startAngle <= endAngle, "Range must have the start angle being before the end angle");

        int startIndex = FindOrMakeStartNode(startAngle, endAngle);
        MergeUntil(startIndex, endAngle);
    }

    /// <summary>
    /// Either finds the node that contains the start angle, creates a new
    /// node that contains the range, or extends a range from a node after
    /// the start angle safely backwards.
    /// </summary>
    /// <remarks>
    /// Return of this function indicates that the start angle is added
    /// successfully, and that a merge step should follow after.
    /// </remarks>
    /// <param name="startAngle">The starting angle.</param>
    /// <param name="endAngle">The ending angle.</param>
    /// <returns>The node that contains the start angle. This is either an
    /// existing node that was expanded to hold the start angle, or a new
    /// node that was allocated for it, or a node that already spanned the
    /// start angle.</returns>
    private int FindOrMakeStartNode(uint startAngle, uint endAngle)
    {
        if (m_spans.Length == 0)
        {
            m_spans.AddUnsafe(new ClipSpan(startAngle, endAngle));
            return 0;
        }

        int index = FindIndexJustAfterOrIncluding(startAngle);

        // If all the nodes end before the starting point, add a new one
        // onto the end.
        if (index == m_spans.Length)
        {
            m_spans.Add(new ClipSpan(startAngle, endAngle));
            return m_spans.Length - 1;
        }

        // startAngle falls inside this span — no insertion needed
        if (m_spans[index].Contains(startAngle))
            return index;

        // If we're in between a gap, we'll make a new node.
        if (m_spans[index].StartAngle > endAngle)
        {
            m_spans.Insert(index, new ClipSpan(startAngle, endAngle));
            return index;
        }

        // We can extend the starting node backwards without worrying about
        // creating an overlap, since `startNode` would have been that node
        // instead if such a node existed.
        m_spans[index] = new ClipSpan(startAngle, m_spans[index].EndAngle);
        return index;
    }

    /// <summary>
    /// Finds the node with the clip span that either includes the start
    /// angle or begins after the start angle (and before any others after
    /// that).
    /// </summary>
    /// <param name="startAngle">The starting angle to find with.</param>
    /// <returns>Either the node that includes or is after the start angle,
    /// or null if there are no nodes that satisfy this criterion (implying
    /// that the start angle is after the end of every span).</returns>
    private int FindIndexJustAfterOrIncluding(uint startAngle)
    {
        // Binary search
        int min = 0;
        int max = m_spans.Length - 1;
        while (min <= max)
        {
            var mid = (min + max) / 2;
            if (m_spans.Data[mid].EndAngle < startAngle)
                min = mid + 1;
            else max = mid - 1;
        }
        return min;
    }

    /// <summary>
    /// Starts at the node provided and goes forward until any nodes after
    /// it are merged with the start node. This fuses all the ranges. Upon
    /// completion of this function, `startNode` will contain the merged
    /// ranges.
    /// </summary>
    /// <param name="startNode">The node we should start at and fuse with
    /// everything afterwards that is before or including endAngle.</param>
    /// <param name="endAngle">The ending angle of the span to add.</param>
    private void MergeUntil(int startIndex, uint endAngle)
    {
        // If we start and end inside the same node, then we're done and
        // have no merging to do.
        if (endAngle <= m_spans[startIndex].EndAngle)
            return;

        uint lastSeenEndAngle = m_spans[startIndex].EndAngle;
        int removeIndex = startIndex + 1;
        int removeCount = 0;

        for (int i = startIndex + 1; i < m_spans.Length; i++)
        {
            ref var span = ref m_spans.Data[i];

            // If the next node starts after our ending point, we're done.
            if (endAngle < span.StartAngle)
                break;

            lastSeenEndAngle = span.EndAngle;
            removeCount++;

            // We do this last because we need to make sure we unlink the
            // node as we will be extending the starting node onwards.
            if (span.Contains(endAngle))
                break;
        }

        if (removeCount > 0)
            m_spans.RemoveRange(removeIndex, removeCount);

        uint newEndAngle = Math.Max(endAngle, lastSeenEndAngle);
        m_spans[startIndex] = new ClipSpan(m_spans[startIndex].StartAngle, newEndAngle);
    }
}
