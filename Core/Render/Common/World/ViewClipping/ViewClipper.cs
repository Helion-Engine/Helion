using System;
using System.Collections;
using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Common.World.ViewClipping
{
    // TODO: Implement this with some kind of discrete fused interval tree for O(lg n)?
    
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
    public class ViewClipper : IEnumerable<ClipSpan>
    {
        private const uint DiamondScale = uint.MaxValue / 4;
        private const uint PiAngle = uint.MaxValue / 2;
        private const double RadiansToDiamondAngleFactor = uint.MaxValue / MathHelper.TwoPi;

        private readonly LinkedList<ClipSpan> m_nodes = new();
        
        /// <summary>
        /// The center point from which we will clip from.
        /// </summary>
        public Vec2D Center { private get; set; } = Vec2D.Zero;

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
        public static uint ToDiamondAngle(in Vec2D start, in Vec2D end)
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
            Vec2D pos = end - start;
            if (pos == Vec2D.Zero)
                return 0;
            
            // TODO: Can we fuse two if statements into one statement somehow?
            if (pos.Y >= 0)
            {
                if (pos.X >= 0)
                    return (uint)(DiamondScale * (pos.Y / (pos.X + pos.Y)));
                return (uint)(DiamondScale * (1 - (pos.X / (-pos.X + pos.Y))));
            }

            if (pos.X < 0)
                return (uint)(DiamondScale * (2 - (pos.Y / (-pos.X - pos.Y))));
            return (uint)(DiamondScale * (3 + (pos.X / (pos.X - pos.Y))));
        }
        
        public static uint DiamondAngleFromRadians(double radians)
        {
            unchecked
            {
                return (uint)(radians * RadiansToDiamondAngleFactor);
            }
        }
        
        /// <summary>
        /// Takes a position and gets the diamond angle value relative to the
        /// last set center spot. The diamond angle is an ordered angle that
        /// is similar to degrees or radians, and has absolute ordering.
        /// </summary>
        /// <param name="vertex">The vertex to convert to a diamond angle.</param>
        /// <returns>The diamond angle for the vertex. This will be zero if it
        /// is equal to the <see cref="Center"/>.</returns>
        public uint GetDiamondAngle(in Vec2D vertex) => ToDiamondAngle(Center, vertex);
        
        /// <summary>
        /// Clears all the clip ranges.
        /// </summary>
        /// <remarks>
        /// Unless you know what you are doing, you should also set the
        /// <see cref="Center"/> variable to a new position.
        /// </remarks>
        public void Clear()
        {
            m_nodes.Clear();
        }

        /// <summary>
        /// Adds two positions that will be converted into angles and then
        /// added to be a clipping range.
        /// </summary>
        /// <param name="first">The first vertex of a line segment.</param>
        /// <param name="second">The second vertex of a line segment.</param>
        public void AddLine(in Vec2D first, in Vec2D second)
        {
            (uint smallerAngle, uint largerAngle) = MathHelper.MinMax(GetDiamondAngle(first), GetDiamondAngle(second));
            
            if (AnglesSpanOriginVector(smallerAngle, largerAngle))
            {
                AddRange(0, smallerAngle);
                AddRange(largerAngle, uint.MaxValue);
            }
            else
                AddRange(smallerAngle, largerAngle);
        }
        
        /// <summary>
        /// Checks if the two points provided are encased in any ranges.
        /// </summary>
        /// <param name="first">The first vertex of a line segment.</param>
        /// <param name="second">The second vertex of a line segment.</param>
        /// <returns>True if they are in a range, false if not.</returns>
        public bool InsideAnyRange(in Vec2D first, in Vec2D second)
        {
            if (m_nodes.Empty())
                return false;
            
            (uint smallerAngle, uint largerAngle) = MathHelper.MinMax(GetDiamondAngle(first), GetDiamondAngle(second));

            if (AnglesSpanOriginVector(smallerAngle, largerAngle))
                return InRange(0, smallerAngle) && InRange(largerAngle, uint.MaxValue);
            return InRange(smallerAngle, largerAngle);
        }
        
        /// <inheritdoc/>
        public IEnumerator<ClipSpan> GetEnumerator() => m_nodes.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
            
            LinkedListNode<ClipSpan>? node = m_nodes.First;
            while (node != null)
            {
                if (node.Value.Contains(startAngle, endAngle))
                    return true;

                if (endAngle < node.Value.StartAngle)
                    return false;
                
                node = node.Next;
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

            LinkedListNode<ClipSpan> startNode = FindOrMakeStartNode(startAngle, endAngle);
            MergeUntil(startNode, endAngle);
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
        private LinkedListNode<ClipSpan> FindOrMakeStartNode(uint startAngle, uint endAngle)
        {
            if (m_nodes.Empty())
                return m_nodes.AddLast(new ClipSpan(startAngle, endAngle));

            LinkedListNode<ClipSpan>? startNode = FindNodeJustAfterOrIncluding(startAngle);
            
            // If all the nodes end before the starting point, add a new one
            // onto the end.
            if (startNode == null)
                return m_nodes.AddLast(new ClipSpan(startAngle, endAngle));

            if (startNode.Value.Contains(startAngle))
                return startNode;
            
            // If we're in between a gap, we'll make a new node.
            if (startNode.Value.StartAngle > endAngle)
                return m_nodes.AddBefore(startNode, new ClipSpan(startAngle, endAngle));
            
            // We can extend the starting node backwards without worrying about
            // creating an overlap, since `startNode` would have been that node
            // instead if such a node existed. 
            startNode.Value = new ClipSpan(startAngle, startNode.Value.EndAngle);
            return startNode;
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
        private LinkedListNode<ClipSpan>? FindNodeJustAfterOrIncluding(uint startAngle)
        {
            LinkedListNode<ClipSpan>? node = m_nodes.First;

            while (node != null)
            {
                if (node.Value.Contains(startAngle) || node.Value.StartAngle >= startAngle)
                    break;
                
                node = node.Next;
            }

            return node;
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
        private void MergeUntil(LinkedListNode<ClipSpan> startNode, uint endAngle)
        {
            // If we start and end inside the same node, then we're done and
            // have no merging to do.
            if (endAngle <= startNode.Value.EndAngle)
                return;

            uint lastSeenNodeEndAngle = startNode.Value.EndAngle;
            LinkedListNode<ClipSpan>? current = startNode.Next;
            
            while (current != null)
            {
                ClipSpan clipSpan = current.Value;
                
                // If the next node starts after our ending point, we're done.
                if (endAngle < clipSpan.StartAngle)
                    break;
                
                lastSeenNodeEndAngle = clipSpan.EndAngle;

                LinkedListNode<ClipSpan>? next = current.Next;
                m_nodes.Remove(current);
                current = next;

                // We do this last because we need to make sure we unlink the
                // node as we will be extending the starting node onwards.
                if (clipSpan.Contains(endAngle))
                    break;
            }

            uint newEndingAngle = Math.Max(endAngle, lastSeenNodeEndAngle);
            startNode.Value = new ClipSpan(startNode.Value.StartAngle, newEndingAngle);
        }
    }
}