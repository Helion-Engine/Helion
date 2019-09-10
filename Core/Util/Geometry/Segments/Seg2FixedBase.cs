using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Segments.Enums;
using Helion.Util.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Geometry.Segments
{
    /// <summary>
    /// The base class of a 2D segment for the type provided.
    /// </summary>
    /// <remarks>
    /// Intended to be only for very basic computations in the constructor. As
    /// there are places which use 'hot' loops where we want to use as little
    /// data as possible so we can fit more in the cache, this gives us the
    /// minimum amount of space we need for basic operations. This class is
    /// best used for temporary shortlived instances.
    /// </remarks>
    public class Seg2FixedBase
    {
        /// <summary>
        /// The beginning point of the segment.
        /// </summary>
        public readonly Vec2Fixed Start;

        /// <summary>
        /// The ending point of the segment.
        /// </summary>
        public readonly Vec2Fixed End;

        /// <summary>
        /// The difference between the start to the end. This means that
        /// Start + Delta = End.
        /// </summary>
        public readonly Vec2Fixed Delta;

        /// <summary>
        /// Creates a new segment. The start and endpoints must be different.
        /// </summary>
        /// <param name="start">The starting point.</param>
        /// <param name="end">The ending point.</param>
        public Seg2FixedBase(Vec2Fixed start, Vec2Fixed end)
        {
            Precondition(start != end, "Segment should not be a point");

            Start = start;
            End = end;
            Delta = end - start;
        }

        /// <summary>
        /// Calculates the 'double triangle' area which is the triangle formed
        /// from the three points, but doubled.
        /// </summary>
        /// <param name="first">The first point.</param>
        /// <param name="second">The second point.</param>
        /// <param name="third">The third point.</param>
        /// <returns>The doubled area of the triangles.</returns>
        public static Fixed DoubleTriArea(Vec2Fixed first, Vec2Fixed second, Vec2Fixed third)
        {
            return ((first.X - third.X) * (second.Y - third.Y)) - ((first.Y - third.Y) * (second.X - third.X));
        }

        /// <summary>
        /// Gets the endpoint based on an index, where 0 = Start and 1 = End.
        /// </summary>
        /// <param name="index">The index of the endpoint.</param>
        /// <returns>The endpoint for the index.</returns>
        public Vec2Fixed this[int index] => index == 0 ? Start : End;

        /// <summary>
        /// Gets the endpoint from the enumeration.
        /// </summary>
        /// <param name="endpoint">The endpoint to get.</param>
        /// <returns>The endpoint for the enumeration.</returns>
        public Vec2Fixed this[Endpoint endpoint] => endpoint == Endpoint.Start ? Start : End;

        /// <summary>
        /// Gets the opposite endpoint from the enumeration.
        /// </summary>
        /// <param name="endpoint">The opposite endpoint to get.</param>
        /// <returns>The opposite endpoint for the enumeration.</returns>
        public Vec2Fixed Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;

        /// <summary>
        /// Gets a point from the time provided. This will also work even if
        /// the time is not in the [0.0, 1.0] range.
        /// </summary>
        /// <param name="t">The time (where 0.0 = start and 1.0 = end).</param>
        /// <returns>The point from the time provided.</returns>
        public Vec2Fixed FromTime(Fixed t) => Start + (Delta * t);

        /// <summary>
        /// Checks if both segments go in the same direction, with respect for
        /// the Start -> End direction.
        /// </summary>
        /// <param name="seg">The other segment to compare against.</param>
        /// <returns>True if they go the same direction, false otherwise.
        /// </returns>
        public bool SameDirection(Seg2FixedBase seg) => SameDirection(seg.Delta);

        /// <summary>
        /// Same as <see cref="SameDirection(Seg2FixedBase)"/> but uses a delta to
        /// check.
        /// </summary>
        /// <param name="delta">The delta direction.</param>
        /// <returns>True if they go the same direction, false otherwise.
        /// </returns>
        public bool SameDirection(Vec2Fixed delta)
        {
            return !MathHelper.DifferentSign(Delta.X, delta.X) && !MathHelper.DifferentSign(Delta.Y, delta.Y);
        }

        /// <summary>
        /// Calculates the perpendicular dot product. This also may be known as
        /// the wedge product.
        /// </summary>
        /// <param name="point">The point to test against.</param>
        /// <returns>The perpendicular dot product.</returns>
        public Fixed PerpDot(Vec2Fixed point)
        {
            return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));
        }

        /// <summary>
        /// Gets the side the point is on relative to this segment.
        /// </summary>
        /// <param name="point">The point to get.</param>
        /// <returns>The side it's on.</returns>
        public Rotation ToSide(Vec2Fixed point)
        {
            Fixed value = PerpDot(point);
            bool approxZero = MathHelper.IsZero(value, Fixed.Epsilon());
            return approxZero ? Rotation.On : (value < 0 ? Rotation.Right : Rotation.Left);
        }

        /// <summary>
        /// Checks if the point is on the right side of this segment (or on the
        /// seg itself).
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if it's on the right (or on the line), false if on 
        /// the left.</returns>
        public bool OnRight(Vec2Fixed point) => PerpDot(point) <= 0;

        /// <summary>
        /// Checks if the segment has both endpoints on this or on the right of
        /// this.
        /// </summary>
        /// <param name="seg">The segment to check.</param>
        /// <returns>True if the segment has both points on/to the right, or
        /// false if one or more points is on the left.</returns>
        public bool OnRight(Seg2FixedBase seg) => OnRight(seg.Start) && OnRight(seg.End);

        /// <summary>
        /// Checks if the box has all the points on the right side.
        /// </summary>
        /// <param name="box">The box to check.</param>
        /// <returns>True if the box has all the points on the right side or
        /// on the segment, false otherwise.</returns>
        public bool OnRight(Box2Fixed box) => OnRight(box.Min);

        /// <summary>
        /// Checks if the two points are on different sides of this segment.
        /// This considers a point on the segment to be on the right side.
        /// </summary>
        /// <param name="first">The first point.</param>
        /// <param name="second">The second point.</param>
        /// <returns>True if they are, false if not.</returns>
        public bool DifferentSides(Vec2Fixed first, Vec2Fixed second) => OnRight(first) != OnRight(second);

        /// <summary>
        /// Checks if the two points of the segment are on different sides of 
        /// this segment. This considers a point on the segment to be on the 
        /// right side.
        /// </summary>
        /// <param name="seg">The segment endpoints to check.</param>
        /// <returns>True if it is, false if not.</returns>
        public bool DifferentSides(Seg2FixedBase seg) => OnRight(seg.Start) != OnRight(seg.End);

        /// <summary>
        /// Checks if the segment provided is parallel.
        /// </summary>
        /// <param name="seg">The segment to check.</param>
        /// <returns>True if it's parallel, false if not.</returns>
        public bool Parallel(Seg2FixedBase seg)
        {
            // If both slopes are the same for seg 1 and 2, then we know the
            // slopes are the same, meaning: d1y / d1x = d2y / d2x. Therefore
            // d1y * d2x == d2y * d1x. This also avoids weird division by zero
            // errors and all that fun stuff from any vertical lines.
            return MathHelper.AreEqual(Delta.Y * seg.Delta.X, Delta.X * seg.Delta.Y, Fixed.Epsilon());
        }

        /// <summary>
        /// Gets the closest distance from the point provided to this segment.
        /// </summary>
        /// <param name="point">The point to evaluate.</param>
        /// <returns>The closest distance.</returns>
        public Fixed ClosestDistance(Vec2Fixed point)
        {
            // Source: https://math.stackexchange.com/questions/2193720/find-a-point-on-a-line-segment-which-is-the-closest-to-other-point-not-on-the-li
            Vec2Fixed pointToStartDelta = Start - point;
            Fixed t = -Delta.Dot(pointToStartDelta) / Delta.Dot(Delta);

            if (t <= 0)
                return point.Distance(Start);
            else if (t >= 1)
                return point.Distance(End);
            return point.Distance(FromTime(t));
        }

        /// <summary>
        /// Checks if an intersection exists. This treats both of the segments
        /// as segments, not as infinite lines.
        /// </summary>
        /// <param name="other">The other segment to check.</param>
        /// <returns>True if an intersection exists, false if not.</returns>
        public bool Intersects(Seg2FixedBase other) => Intersection(other, out Fixed t) && (t >= 0 && t <= 1);

        /// <summary>
        /// Gets the intersection with a segment. This is not intended for line
        /// extension intersection, see the '...AsLine() methods for that.
        /// </summary>
        /// <remarks>
        /// See <see cref="IntersectionAsLine(Seg2FixedBase, out Fixed)"/> for one
        /// and <see cref="IntersectionAsLine(Seg2FixedBase, out Fixed, out Fixed)"/>
        /// for both intersection times.
        /// </remarks>
        /// <param name="seg">The segment to check.</param>
        /// <param name="t">The output intersection time. If this function
        /// returns false then it will have a default value.</param>
        /// <returns>True if they intersect, false if not.</returns>
        public bool Intersection(Seg2FixedBase seg, out Fixed t)
        {
            Fixed areaStart = DoubleTriArea(Start, End, seg.End);
            Fixed areaEnd = DoubleTriArea(Start, End, seg.Start);

            if (MathHelper.DifferentSign(areaStart, areaEnd))
            {
                Fixed areaThisStart = DoubleTriArea(seg.Start, seg.End, Start);
                Fixed areaThisEnd = DoubleTriArea(seg.Start, seg.End, End);

                if (MathHelper.DifferentSign(areaStart, areaEnd))
                {
                    t = areaThisStart / (areaThisStart - areaThisEnd);
                    return true;
                }
            }

            t = default;
            return false;
        }

        /// <summary>
        /// Treats intersection as if they are lines, so intersection points
        /// from this function are possibly found outside of the [0, 1] range.
        /// </summary>
        /// <param name="seg">The segment to test against.</param>
        /// <param name="tThis">The time of intersection located on this
        /// segment (not the parameter one). This has a default value if the
        /// method returns false.</param>
        /// <returns>True if an intersection exists, false if not.</returns>
        public bool IntersectionAsLine(Seg2FixedBase seg, out Fixed tThis)
        {
            Fixed determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
            if (MathHelper.IsZero(determinant))
            {
                tThis = default;
                return false;
            }

            Vec2Fixed startDelta = Start - seg.Start;
            tThis = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) / determinant;
            return true;
        }

        /// <summary>
        /// Treats intersection as if they are lines, so intersection points
        /// from this function are possibly found outside of the [0, 1] range.
        /// </summary>
        /// <param name="seg">The segment to test against.</param>
        /// <param name="tThis">The time of intersection located on this
        /// segment (not the parameter one). This has a default value if the
        /// method returns false.</param>
        /// <param name="tOther">Same as `tThis`, but for the other segment.
        /// </param>
        /// <returns>True if an intersection exists, false if not.</returns>
        public bool IntersectionAsLine(Seg2FixedBase seg, out Fixed tThis, out Fixed tOther)
        {
            Fixed determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
            if (MathHelper.IsZero(determinant))
            {
                tThis = default;
                tOther = default;
                return false;
            }

            Vec2Fixed startDelta = Start - seg.Start;
            Fixed inverseDeterminant = determinant.Inverse();
            tThis = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) * inverseDeterminant;
            tOther = ((-Delta.Y * startDelta.X) + (Delta.X * startDelta.Y)) * inverseDeterminant;
            return true;
        }

        /// <summary>
        /// Gets the length of the segment.
        /// </summary>
        /// <returns>The length of the segment.</returns>
        public Fixed Length() => Delta.Length();

        /// <summary>
        /// Gets the squared length of the segment.
        /// </summary>
        /// <returns>The squared length of the segment.</returns>
        public Fixed LengthSquared() => Delta.LengthSquared();
        
        /// <summary>
        /// Gets the normal for this segment, which is equal to rotating the
        /// delta to the right by 90 degrees.
        /// </summary>
        /// <returns>The 90 degree right angle rotation of the delta.</returns>
        public Vec2Fixed RightNormal() => Delta.OriginRightRotate90();
    }
}
