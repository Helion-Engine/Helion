using System;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments.Enums;
using Helion.Geometry.Vectors;
using Helion.Util;

namespace Helion.Geometry.Segments
{
    /// <summary>
    /// The full class of a 2D segment for the type provided.
    /// </summary>
    /// <remarks>
    /// Contains more members for when we expect to reuse certainl values that
    /// have a computational cost of calculating. This should be the first one
    /// to use instead of the 'base' version it inherits from, unless there is
    /// proof in the profiler that this has some bottleneck.
    /// </remarks>
    public class Seg2D : Seg2DBase
    {
        /// <summary>
        /// The inversed components of the delta.
        /// </summary>
        public readonly Vec2D DeltaInverse;

        /// <summary>
        /// The bounding box of this segment.
        /// </summary>
        public readonly Box2D Box;

        /// <summary>
        /// The direction this segment goes.
        /// </summary>
        public readonly SegmentDirection Direction;

        /// <summary>
        /// Creates a new segment. The start and endpoints must be different.
        /// </summary>
        /// <param name="start">The starting point.</param>
        /// <param name="end">The ending point.</param>
        public Seg2D(in Vec2D start, in Vec2D end) : base(start, end)
        {
            DeltaInverse = new Vec2D(1.0f / Delta.X, 1.0f / Delta.Y);
            Box = MakeBox(start, end);
            Direction = CalculateDirection(Delta);
        }

        /// <summary>
        /// Gets the rotation from a point with respect to another two points
        /// that make a line.
        /// </summary>
        /// <remarks>
        /// <para>Calculates the side the third point is on.</para>
        /// <para>This assumes that `first` and `second` form a line segment (where first
        /// is the starting point and second is the ending point of the segment) and
        /// the third point is evaluated to be on the side of the line from the two
        /// points. It can be imagined like this:</para>
        /// <code>
        ///                |
        ///    Second o---------o First
        ///         _/
        ///         /      (rotation would be on the left side)
        ///  Third o
        /// </code>
        /// </remarks>
        /// <param name="first">The first point.</param>
        /// <param name="second">The second point between first/third.</param>
        /// <param name="third">The third point.</param>
        /// <param name="epsilon">An optional comparison value.</param>
        /// <returns>The side the third point is on relative to the first and
        /// the second point.</returns>
        public static Rotation Rotation(in Vec2D first, in Vec2D second, in Vec2D third, double epsilon = 0.000001)
        {
            return new Seg2DBase(first, second).ToSide(third, epsilon);
        }

        /// <summary>
        /// Gets the time the point would have on the segment. This does not
        /// need to be between the [0, 1] range.
        /// </summary>
        /// <remarks>
        /// If the point is not on the segment, then the result will be wrong.
        /// A corollary to this is that `Start + t*Delta = point`.
        /// </remarks>
        /// <param name="point">The point to get the time for.</param>
        /// <returns>The time the point is on this segment.</returns>
        public double ToTime(in Vec2D point)
        {
            if (!MathHelper.IsZero(Delta.X))
                return (point.X - Start.X) * DeltaInverse.X;
            return (point.Y - Start.Y) * DeltaInverse.Y;
        }

        /// <summary>
        /// Checks if the segments overlap. This assumes collinearity.
        /// </summary>
        /// <param name="seg">The segment to check.</param>
        /// <returns>True if they overlap, false otherwise.</returns>
        public bool Overlaps(in Seg2DBase seg)
        {
            double tStart = ToTime(seg.Start);
            double tEnd = ToTime(seg.End);
            return (tStart > 0 && tStart < 1) || (tEnd > 0 && tEnd < 1);
        }

        /// <summary>
        /// Checks if the box intersects this segment.
        /// </summary>
        /// <param name="box">The box to check.</param>
        /// <returns>True if it intersects, false if not.</returns>
        public bool Intersects(in Box2D box)
        {
            if (!Box.Overlaps(box))
                return false;

            switch (Direction)
            {
            case SegmentDirection.Vertical:
                return box.Min.X < Start.X && Start.X < box.Max.X;
            case SegmentDirection.Horizontal:
                return box.Min.Y < Start.Y && Start.Y < box.Max.Y;
            case SegmentDirection.PositiveSlope:
                return DifferentSides(box.TopLeft, box.BottomRight);
            case SegmentDirection.NegativeSlope:
                return DifferentSides(box.BottomLeft, box.TopRight);
            default:
                throw new InvalidOperationException("Invalid box intersection direction enumeration");
            }
        }

        private static Box2D MakeBox(in Vec2D start, in Vec2D end)
        {
            return new Box2D(
                new Vec2D(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y)),
                new Vec2D(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y)));
        }

        private static SegmentDirection CalculateDirection(in Vec2D delta)
        {
            if (MathHelper.IsZero(delta.X))
                return SegmentDirection.Vertical;
            if (MathHelper.IsZero(delta.Y))
                return SegmentDirection.Horizontal;
            return MathHelper.DifferentSign(delta.X, delta.Y) ? SegmentDirection.NegativeSlope : SegmentDirection.PositiveSlope;
        }
    }
}