using System;
using System.Numerics;

namespace Helion.Util.Geometry
{
    // TODO: Would be nice to make this a .tt file and autogenerate it. However
    // there are some slight changed in Seg2Fixed[Base] that make it no longer
    // identical to the others.

    /// <summary>
    /// The direction a segment goes in from Start -> End.
    /// </summary>
    public enum SegmentDirection
    {
        Vertical,
        Horizontal,
        PositiveSlope,
        NegativeSlope
    }

    /// <summary>
    /// A rotation with respect to a segment.
    /// </summary>
    public enum Rotation
    {
        Left,
        On,
        Right
    }

    /// <summary>
    /// A simple enumeration for representing endpoints of a segment.
    /// </summary>
    public enum Endpoint
    {
        Start,
        End
    }

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
    public class Seg2FBase
    {
        /// <summary>
        /// The beginning point of the segment.
        /// </summary>
        public readonly Vector2 Start;

        /// <summary>
        /// The ending point of the segment.
        /// </summary>
        public readonly Vector2 End;

        /// <summary>
        /// The difference between the start to the end. This means that
        /// Start + Delta = End.
        /// </summary>
        public readonly Vector2 Delta;

        /// <summary>
        /// Creates a new segment. The start and endpoints must be different.
        /// </summary>
        /// <param name="start">The starting point.</param>
        /// <param name="end">The ending point.</param>
        public Seg2FBase(Vector2 start, Vector2 end)
        {
            Assert.Precondition(start != end, "Segment should not be a point");

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
        public static float DoubleTriArea(Vector2 first, Vector2 second, Vector2 third)
        {
            return ((first.X - third.X) * (second.Y - third.Y)) - ((first.Y - third.Y) * (second.X - third.X));
        }

        /// <summary>
        /// Gets the endpoint based on an index, where 0 = Start and 1 = End.
        /// </summary>
        /// <param name="index">The index of the endpoint.</param>
        /// <returns>The endpoint for the index.</returns>
        public Vector2 this[int index] => index == 0 ? Start : End;

        /// <summary>
        /// Gets the endpoint from the enumeration.
        /// </summary>
        /// <param name="endpoint">The endpoint to get.</param>
        /// <returns>The endpoint for the enumeration.</returns>
        public Vector2 this[Endpoint endpoint] => endpoint == Endpoint.Start ? Start : End;

        /// <summary>
        /// Gets the opposite endpoint from the enumeration.
        /// </summary>
        /// <param name="endpoint">The opposite endpoint to get.</param>
        /// <returns>The opposite endpoint for the enumeration.</returns>
        public Vector2 Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;

        /// <summary>
        /// Gets a point from the time provided. This will also work even if
        /// the time is not in the [0.0, 1.0] range.
        /// </summary>
        /// <param name="t">The time (where 0.0 = start and 1.0 = end)</param>
        /// <returns>The point from the time provided.</returns>
        public Vector2 FromTime(float t) => Start + (Delta * t);

        /// <summary>
        /// Checks if both segments go in the same direction, with respect for
        /// the Start -> End direction.
        /// </summary>
        /// <param name="seg">The other segment to compare against.</param>
        /// <returns>True if they go the same direction, false otherwise.
        /// </returns>
        public bool SameDirection(Seg2FBase seg) => SameDirection(seg.Delta);

        /// <summary>
        /// Same as <see cref="SameDirection(Seg2FBase)"/> but uses a delta to
        /// check.
        /// </summary>
        /// <param name="delta">The delta direction.</param>
        /// <returns>True if they go the same direction, false otherwise.
        /// </returns>
        public bool SameDirection(Vector2 delta)
        {
            return !MathHelper.DifferentSign(Delta.X, delta.X) && !MathHelper.DifferentSign(Delta.Y, delta.Y);
        }

        /// <summary>
        /// Calculates the perpendicular dot product. This also may be known as
        /// the wedge product.
        /// </summary>
        /// <param name="point">The point to test against.</param>
        /// <returns>The perpendicular dot product.</returns>
        public float PerpDot(Vector2 point)
        {
            return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));
        }

        /// <summary>
        /// Gets the side the point is on relative to this segment.
        /// </summary>
        /// <param name="point">The point to get.</param>
        /// <param name="epsilon">An optional epsilon for comparison.</param>
        /// <returns>The side it's on.</returns>
        public Rotation ToSide(Vector2 point, float epsilon = 0.00001f)
        {
            float value = PerpDot(point);
            bool approxZero = MathHelper.IsZero(value, epsilon);
            return approxZero ? Rotation.On : (value < 0 ? Rotation.Right : Rotation.Left);
        }

        /// <summary>
        /// Checks if the point is on the right side of this segment (or on the
        /// seg itself).
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if it's on the right (or on the line), false if on 
        /// the left.</returns>
        public bool OnRight(Vector2 point) => PerpDot(point) <= 0;

        /// <summary>
        /// Checks if the segment has both endpoints on this or on the right of
        /// this.
        /// </summary>
        /// <param name="seg">The segment to check.</param>
        /// <returns>True if the segment has both points on/to the right, or
        /// false if one or more points is on the left.</returns>
        public bool OnRight(Seg2FBase seg) => OnRight(seg.Start) && OnRight(seg.End);

        /// <summary>
        /// Checks if the box has all the points on the right side (or on the
        /// segment).
        /// </summary>
        /// <param name="box">The box to check.</param>
        /// <returns>True if the box has all the points on the right side or
        /// on the segment, false otherwise.</returns>
        public bool OnRight(Box2F box)
        {
            return OnRight(box.BottomLeft) && 
                   OnRight(box.BottomRight) &&
                   OnRight(box.TopLeft) &&
                   OnRight(box.TopRight);
        }

        /// <summary>
        /// Checks if the two points are on different sides of this segment.
        /// This considers a point on the segment to be on the right side.
        /// </summary>
        /// <param name="first">The first point.</param>
        /// <param name="second">The second point.</param>
        /// <returns>True if they are, false if not.</returns>
        public bool DifferentSides(Vector2 first, Vector2 second) => OnRight(first) != OnRight(second);

        /// <summary>
        /// Checks if the two points of the segment are on different sides of 
        /// this segment. This considers a point on the segment to be on the 
        /// right side.
        /// </summary>
        /// <param name="seg">The segment endpoints to check.</param>
        /// <returns>True if it is, false if not.</returns>
        public bool DifferentSides(Seg2FBase seg) => OnRight(seg.Start) != OnRight(seg.End);

        /// <summary>
        /// Checks if the segment provided is parallel.
        /// </summary>
        /// <param name="seg">The segment to check.</param>
        /// <param name="epsilon">An optional comparison epsilon.</param>
        /// <returns>True if it's parallel, false if not.</returns>
        public bool Parallel(Seg2FBase seg, float epsilon = 0.00001f)
        {
            // If both slopes are the same for seg 1 and 2, then we know the
            // slopes are the same, meaning: d1y / d1x = d2y / d2x. Therefore
            // d1y * d2x == d2y * d1x. This also avoids weird division by zero
            // errors and all that fun stuff from any vertical lines.
            return MathHelper.AreEqual(Delta.Y * seg.Delta.X, Delta.X * seg.Delta.Y, epsilon);
        }

        /// <summary>
        /// Gets the closest distance from the point provided to this segment.
        /// </summary>
        /// <param name="point">The point to evaluate.</param>
        /// <returns>The closest distance.</returns>
        public float ClosestDistance(Vector2 point)
        {
            // Source: https://math.stackexchange.com/questions/2193720/find-a-point-on-a-line-segment-which-is-the-closest-to-other-point-not-on-the-li
            Vector2 pointToStartDelta = Start - point;
            float t = -Delta.Dot(pointToStartDelta) / Delta.Dot(Delta);

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
        public bool Intersects(Seg2FBase other) => Intersection(other, out float t) ? (0 <= t && t <= 1) : false;

        /// <summary>
        /// Gets the intersection with a segment. This is not intended for line
        /// extension intersection, see the '...AsLine() methods for that.
        /// </summary>
        /// <remarks>
        /// See <see cref="IntersectionAsLine(Seg2FBase, out float)"/> for one
        /// and <see cref="IntersectionAsLine(Seg2FBase, out float, out float)"/>
        /// for both intersection times.
        /// </remarks>
        /// <param name="seg">The segment to check.</param>
        /// <param name="t">The output intersection time. If this function
        /// returns false then it will have a default value.</param>
        /// <returns>True if they intersect, false if not.</returns>
        public bool Intersection(Seg2FBase seg, out float t)
        {
            float areaStart = DoubleTriArea(Start, End, seg.End);
            float areaEnd = DoubleTriArea(Start, End, seg.Start);

            if (MathHelper.DifferentSign(areaStart, areaEnd))
            {
                float areaThisStart = DoubleTriArea(seg.Start, seg.End, Start);
                float areaThisEnd = DoubleTriArea(seg.Start, seg.End, End);

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
        public bool IntersectionAsLine(Seg2FBase seg, out float tThis)
        {
            float determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
            if (MathHelper.IsZero(determinant))
            {
                tThis = default;
                return false;
            }

            Vector2 startDelta = Start - seg.Start;
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
        public bool IntersectionAsLine(Seg2FBase seg, out float tThis, out float tOther)
        {
            float determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
            if (MathHelper.IsZero(determinant))
            {
                tThis = default;
                tOther = default;
                return false;
            }

            Vector2 startDelta = Start - seg.Start;
            float inverseDeterminant = 1.0f / determinant;
            tThis = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) * inverseDeterminant;
            tOther = ((-Delta.Y * startDelta.X) + (Delta.X * startDelta.Y)) * inverseDeterminant;
            return true;
        }

        /// <summary>
        /// Gets the length of the segment.
        /// </summary>
        /// <returns>The length of the segment</returns>
        public float Length() => Delta.Length();

        /// <summary>
        /// Gets the squared length of the segment.
        /// </summary>
        /// <returns>The squared length of the segment</returns>
        public float LengthSquared() => Delta.LengthSquared();
    }

    /// <summary>
    /// The full class of a 2D segment for the type provided.
    /// </summary>
    /// <remarks>
    /// Contains more members for when we expect to reuse certainl values that
    /// have a computational cost of calculating. This should be the first one
    /// to use instead of the 'base' version it inherits from, unless there is
    /// proof in the profiler that this has some bottleneck.
    /// </remarks>
    public class Seg2F : Seg2FBase
    {
        /// <summary>
        /// The inversed components of the delta.
        /// </summary>
        public readonly Vector2 DeltaInverse;

        /// <summary>
        /// The bounding box of this segment.
        /// </summary>
        public readonly Box2F Box;

        /// <summary>
        /// The direction this segment goes.
        /// </summary>
        public readonly SegmentDirection Direction;

        /// <summary>
        /// Creates a new segment. The start and endpoints must be different.
        /// </summary>
        /// <param name="start">The starting point.</param>
        /// <param name="end">The ending point.</param>
        public Seg2F(Vector2 start, Vector2 end) : base(start, end)
        {
            DeltaInverse = new Vector2(1.0f / Delta.X, 1.0f / Delta.Y);
            Box = MakeBox(start, end);
            Direction = CalculateDirection(Delta);
        }

        private static Box2F MakeBox(Vector2 start, Vector2 end)
        {
            return new Box2F(
                new Vector2(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y)),
                new Vector2(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y))
            );
        }

        private static SegmentDirection CalculateDirection(Vector2 delta)
        {
            if (MathHelper.IsZero(delta.X))
                return SegmentDirection.Vertical;
            if (MathHelper.IsZero(delta.Y))
                return SegmentDirection.Horizontal;
            return MathHelper.DifferentSign(delta.X, delta.Y) ? SegmentDirection.NegativeSlope : SegmentDirection.PositiveSlope;
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
        public static Rotation Rotation(Vector2 first, Vector2 second, Vector2 third, float epsilon = 0.00001f)
        {
            return new Seg2FBase(first, second).ToSide(third, epsilon);
        }

        /// <summary>
        /// Gets the time the point would have on the segment. This does not
        /// need to be between the [0, 1] range.
        /// </summary>
        /// <remarks>
        /// If the point is not on the segment, then the result will be wrong.
        /// A corollary to this is that <code>Start + t*Delta = point</code>.
        /// </remarks>
        /// <param name="point">The point to get the time for.</param>
        /// <returns>The time the point is on this segment.</returns>
        public float ToTime(Vector2 point)
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
        public bool Overlaps(Seg2FBase seg)
        {
            float tStart = ToTime(seg.Start);
            float tEnd = ToTime(seg.End);
            return (tStart > 0 && tStart < 1) || (tEnd > 0 && tEnd < 1);
        }

        /// <summary>
        /// Checks if the box intersects this segment.
        /// </summary>
        /// <param name="box">The box to check.</param>
        /// <returns>True if it intersects, false if not.</returns>
        public bool Intersects(Box2F box)
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

        /// <summary>
        /// Checks if the segments are collinear to each other.
        /// </summary>
        /// <param name="seg">The segment to check.</param>
        /// <param name="epsilon">The optional epsilon for comparisons.</param>
        /// <returns>True if collinear, false if not.</returns>
        public bool Collinear(Seg2FBase seg, float epsilon = 0.0001f)
        {
            // If the midpoint of the provided segment is on the current segment
            // line, it's reasonably collinear.
            Vector2 midpoint = (seg.Start + seg.End) / 2;
            Vector2 expectedMidpoint = FromTime(ToTime(midpoint));
            return midpoint.EqualTo(expectedMidpoint, epsilon);
        }

        /// <summary>
        /// Gets the right rotated normal with respect to the direction of this
        /// segment.
        /// </summary>
        /// <returns>The 90 degree right angle rotation of this line, and is
        /// normalized.</returns>
        public Vector2 RightRotateNormal() => new Vector2(Delta.Y, -Delta.X).Unit();
    }

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
    public class Seg2DBase
    {
        /// <summary>
        /// The beginning point of the segment.
        /// </summary>
        public readonly Vec2D Start;

        /// <summary>
        /// The ending point of the segment.
        /// </summary>
        public readonly Vec2D End;

        /// <summary>
        /// The difference between the start to the end. This means that
        /// Start + Delta = End.
        /// </summary>
        public readonly Vec2D Delta;

        /// <summary>
        /// Creates a new segment. The start and endpoints must be different.
        /// </summary>
        /// <param name="start">The starting point.</param>
        /// <param name="end">The ending point.</param>
        public Seg2DBase(Vec2D start, Vec2D end)
        {
            Assert.Precondition(start != end, "Segment should not be a point");

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
        public static double DoubleTriArea(Vec2D first, Vec2D second, Vec2D third)
        {
            return ((first.X - third.X) * (second.Y - third.Y)) - ((first.Y - third.Y) * (second.X - third.X));
        }

        /// <summary>
        /// Gets the endpoint based on an index, where 0 = Start and 1 = End.
        /// </summary>
        /// <param name="index">The index of the endpoint.</param>
        /// <returns>The endpoint for the index.</returns>
        public Vec2D this[int index] => index == 0 ? Start : End;

        /// <summary>
        /// Gets the endpoint from the enumeration.
        /// </summary>
        /// <param name="endpoint">The endpoint to get.</param>
        /// <returns>The endpoint for the enumeration.</returns>
        public Vec2D this[Endpoint endpoint] => endpoint == Endpoint.Start ? Start : End;

        /// <summary>
        /// Gets the opposite endpoint from the enumeration.
        /// </summary>
        /// <param name="endpoint">The opposite endpoint to get.</param>
        /// <returns>The opposite endpoint for the enumeration.</returns>
        public Vec2D Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;

        /// <summary>
        /// Gets a point from the time provided. This will also work even if
        /// the time is not in the [0.0, 1.0] range.
        /// </summary>
        /// <param name="t">The time (where 0.0 = start and 1.0 = end)</param>
        /// <returns>The point from the time provided.</returns>
        public Vec2D FromTime(double t) => Start + (Delta * t);

        /// <summary>
        /// Checks if both segments go in the same direction, with respect for
        /// the Start -> End direction.
        /// </summary>
        /// <param name="seg">The other segment to compare against.</param>
        /// <returns>True if they go the same direction, false otherwise.
        /// </returns>
        public bool SameDirection(Seg2DBase seg) => SameDirection(seg.Delta);

        /// <summary>
        /// Same as <see cref="SameDirection(Seg2DBase)"/> but uses a delta to
        /// check.
        /// </summary>
        /// <param name="delta">The delta direction.</param>
        /// <returns>True if they go the same direction, false otherwise.
        /// </returns>
        public bool SameDirection(Vec2D delta)
        {
            return !MathHelper.DifferentSign(Delta.X, delta.X) && !MathHelper.DifferentSign(Delta.Y, delta.Y);
        }

        /// <summary>
        /// Calculates the perpendicular dot product. This also may be known as
        /// the wedge product.
        /// </summary>
        /// <param name="point">The point to test against.</param>
        /// <returns>The perpendicular dot product.</returns>
        public double PerpDot(Vec2D point)
        {
            return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));
        }

        /// <summary>
        /// Gets the side the point is on relative to this segment.
        /// </summary>
        /// <param name="point">The point to get.</param>
        /// <param name="epsilon">An optional epsilon for comparison.</param>
        /// <returns>The side it's on.</returns>
        public Rotation ToSide(Vec2D point, double epsilon = 0.000001)
        {
            double value = PerpDot(point);
            bool approxZero = MathHelper.IsZero(value, epsilon);
            return approxZero ? Rotation.On : (value < 0 ? Rotation.Right : Rotation.Left);
        }

        /// <summary>
        /// Checks if the point is on the right side of this segment (or on the
        /// seg itself).
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if it's on the right (or on the line), false if on 
        /// the left.</returns>
        public bool OnRight(Vec2D point) => PerpDot(point) <= 0;

        /// <summary>
        /// Checks if the segment has both endpoints on this or on the right of
        /// this.
        /// </summary>
        /// <param name="seg">The segment to check.</param>
        /// <returns>True if the segment has both points on/to the right, or
        /// false if one or more points is on the left.</returns>
        public bool OnRight(Seg2DBase seg) => OnRight(seg.Start) && OnRight(seg.End);

        /// <summary>
        /// Checks if the box has all the points on the right side (or on the
        /// segment).
        /// </summary>
        /// <param name="box">The box to check.</param>
        /// <returns>True if the box has all the points on the right side or
        /// on the segment, false otherwise.</returns>
        public bool OnRight(Box2D box)
        {
            return OnRight(box.BottomLeft) &&
                   OnRight(box.BottomRight) &&
                   OnRight(box.TopLeft) &&
                   OnRight(box.TopRight);
        }

        /// <summary>
        /// Checks if the two points are on different sides of this segment.
        /// This considers a point on the segment to be on the right side.
        /// </summary>
        /// <param name="first">The first point.</param>
        /// <param name="second">The second point.</param>
        /// <returns>True if they are, false if not.</returns>
        public bool DifferentSides(Vec2D first, Vec2D second) => OnRight(first) != OnRight(second);

        /// <summary>
        /// Checks if the two points of the segment are on different sides of 
        /// this segment. This considers a point on the segment to be on the 
        /// right side.
        /// </summary>
        /// <param name="seg">The segment endpoints to check.</param>
        /// <returns>True if it is, false if not.</returns>
        public bool DifferentSides(Seg2DBase seg) => OnRight(seg.Start) != OnRight(seg.End);

        /// <summary>
        /// Checks if the segment provided is parallel.
        /// </summary>
        /// <param name="seg">The segment to check.</param>
        /// <param name="epsilon">An optional comparison epsilon.</param>
        /// <returns>True if it's parallel, false if not.</returns>
        public bool Parallel(Seg2DBase seg, double epsilon = 0.000001)
        {
            // If both slopes are the same for seg 1 and 2, then we know the
            // slopes are the same, meaning: d1y / d1x = d2y / d2x. Therefore
            // d1y * d2x == d2y * d1x. This also avoids weird division by zero
            // errors and all that fun stuff from any vertical lines.
            return MathHelper.AreEqual(Delta.Y * seg.Delta.X, Delta.X * seg.Delta.Y, epsilon);
        }

        /// <summary>
        /// Gets the closest distance from the point provided to this segment.
        /// </summary>
        /// <param name="point">The point to evaluate.</param>
        /// <returns>The closest distance.</returns>
        public double ClosestDistance(Vec2D point)
        {
            // Source: https://math.stackexchange.com/questions/2193720/find-a-point-on-a-line-segment-which-is-the-closest-to-other-point-not-on-the-li
            Vec2D pointToStartDelta = Start - point;
            double t = -Delta.Dot(pointToStartDelta) / Delta.Dot(Delta);

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
        public bool Intersects(Seg2DBase other) => Intersection(other, out double t) ? (0 <= t && t <= 1) : false;

        /// <summary>
        /// Gets the intersection with a segment. This is not intended for line
        /// extension intersection, see the '...AsLine() methods for that.
        /// </summary>
        /// <remarks>
        /// See <see cref="IntersectionAsLine(Seg2DBase, out double)"/> for one
        /// and <see cref="IntersectionAsLine(Seg2DBase, out double, out double)"/>
        /// for both intersection times.
        /// </remarks>
        /// <param name="seg">The segment to check.</param>
        /// <param name="t">The output intersection time. If this function
        /// returns false then it will have a default value.</param>
        /// <returns>True if they intersect, false if not.</returns>
        public bool Intersection(Seg2DBase seg, out double t)
        {
            double areaStart = DoubleTriArea(Start, End, seg.End);
            double areaEnd = DoubleTriArea(Start, End, seg.Start);

            if (MathHelper.DifferentSign(areaStart, areaEnd))
            {
                double areaThisStart = DoubleTriArea(seg.Start, seg.End, Start);
                double areaThisEnd = DoubleTriArea(seg.Start, seg.End, End);

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
        public bool IntersectionAsLine(Seg2DBase seg, out double tThis)
        {
            double determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
            if (MathHelper.IsZero(determinant))
            {
                tThis = default;
                return false;
            }

            Vec2D startDelta = Start - seg.Start;
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
        public bool IntersectionAsLine(Seg2DBase seg, out double tThis, out double tOther)
        {
            double determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
            if (MathHelper.IsZero(determinant))
            {
                tThis = default;
                tOther = default;
                return false;
            }

            Vec2D startDelta = Start - seg.Start;
            double inverseDeterminant = 1.0f / determinant;
            tThis = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) * inverseDeterminant;
            tOther = ((-Delta.Y * startDelta.X) + (Delta.X * startDelta.Y)) * inverseDeterminant;
            return true;
        }

        /// <summary>
        /// Gets the length of the segment.
        /// </summary>
        /// <returns>The length of the segment</returns>
        public double Length() => Delta.Length();

        /// <summary>
        /// Gets the squared length of the segment.
        /// </summary>
        /// <returns>The squared length of the segment</returns>
        public double LengthSquared() => Delta.LengthSquared();
    }

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
        public Seg2D(Vec2D start, Vec2D end) : base(start, end)
        {
            DeltaInverse = new Vec2D(1.0f / Delta.X, 1.0f / Delta.Y);
            Box = MakeBox(start, end);
            Direction = CalculateDirection(Delta);
        }

        private static Box2D MakeBox(Vec2D start, Vec2D end)
        {
            return new Box2D(
                new Vec2D(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y)),
                new Vec2D(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y))
            );
        }

        private static SegmentDirection CalculateDirection(Vec2D delta)
        {
            if (MathHelper.IsZero(delta.X))
                return SegmentDirection.Vertical;
            if (MathHelper.IsZero(delta.Y))
                return SegmentDirection.Horizontal;
            return MathHelper.DifferentSign(delta.X, delta.Y) ? SegmentDirection.NegativeSlope : SegmentDirection.PositiveSlope;
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
        public static Rotation Rotation(Vec2D first, Vec2D second, Vec2D third, double epsilon = 0.000001)
        {
            return new Seg2DBase(first, second).ToSide(third, epsilon);
        }

        /// <summary>
        /// Gets the time the point would have on the segment. This does not
        /// need to be between the [0, 1] range.
        /// </summary>
        /// <remarks>
        /// If the point is not on the segment, then the result will be wrong.
        /// A corollary to this is that <code>Start + t*Delta = point</code>.
        /// </remarks>
        /// <param name="point">The point to get the time for.</param>
        /// <returns>The time the point is on this segment.</returns>
        public double ToTime(Vec2D point)
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
        public bool Overlaps(Seg2DBase seg)
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
        public bool Intersects(Box2D box)
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

        /// <summary>
        /// Checks if the segments are collinear to each other.
        /// </summary>
        /// <param name="seg">The segment to check.</param>
        /// <param name="epsilon">The optional epsilon for comparisons.</param>
        /// <returns>True if collinear, false if not.</returns>
        public bool Collinear(Seg2DBase seg, double epsilon = 0.000001)
        {
            // If the midpoint of the provided segment is on the current segment
            // line, it's reasonably collinear.
            Vec2D midpoint = (seg.Start + seg.End) / 2;
            Vec2D expectedMidpoint = FromTime(ToTime(midpoint));
            return midpoint.EqualTo(expectedMidpoint, epsilon);
        }

        /// <summary>
        /// Gets the right rotated normal with respect to the direction of this
        /// segment.
        /// </summary>
        /// <returns>The 90 degree right angle rotation of this line, and is
        /// normalized.</returns>
        public Vec2D RightRotateNormal() => new Vec2D(Delta.Y, -Delta.X).Unit();
    }

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
            Assert.Precondition(start != end, "Segment should not be a point");

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
        /// <param name="t">The time (where 0.0 = start and 1.0 = end)</param>
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
        /// Checks if the box has all the points on the right side (or on the
        /// segment).
        /// </summary>
        /// <param name="box">The box to check.</param>
        /// <returns>True if the box has all the points on the right side or
        /// on the segment, false otherwise.</returns>
        public bool OnRight(Box2Fixed box)
        {
            return OnRight(box.BottomLeft) &&
                   OnRight(box.BottomRight) &&
                   OnRight(box.TopLeft) &&
                   OnRight(box.TopRight);
        }

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
        public bool Intersects(Seg2FixedBase other) => Intersection(other, out Fixed t) ? (t >= 0 && t <= 1) : false;

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
        /// <returns>The length of the segment</returns>
        public Fixed Length() => Delta.Length();

        /// <summary>
        /// Gets the squared length of the segment.
        /// </summary>
        /// <returns>The squared length of the segment</returns>
        public Fixed LengthSquared() => Delta.LengthSquared();
    }

    /// <summary>
    /// The full class of a 2D segment for the type provided.
    /// </summary>
    /// <remarks>
    /// Contains more members for when we expect to reuse certainl values that
    /// have a computational cost of calculating. This should be the first one
    /// to use instead of the 'base' version it inherits from, unless there is
    /// proof in the profiler that this has some bottleneck.
    /// </remarks>
    public class Seg2Fixed : Seg2FixedBase
    {
        /// <summary>
        /// The inversed components of the delta.
        /// </summary>
        public readonly Vec2Fixed DeltaInverse;

        /// <summary>
        /// The bounding box of this segment.
        /// </summary>
        public readonly Box2Fixed Box;

        /// <summary>
        /// The direction this segment goes.
        /// </summary>
        public readonly SegmentDirection Direction;

        /// <summary>
        /// Creates a new segment. The start and endpoints must be different.
        /// </summary>
        /// <param name="start">The starting point.</param>
        /// <param name="end">The ending point.</param>
        public Seg2Fixed(Vec2Fixed start, Vec2Fixed end) : base(start, end)
        {
            DeltaInverse = new Vec2Fixed(Delta.X.Inverse(), Delta.Y.Inverse());
            Box = MakeBox(start, end);
            Direction = CalculateDirection(Delta);
        }

        private static Box2Fixed MakeBox(Vec2Fixed start, Vec2Fixed end)
        {
            return new Box2Fixed(
                new Vec2Fixed(
                    new Fixed(Math.Min(start.X.Bits, end.X.Bits)), 
                    new Fixed(Math.Min(start.Y.Bits, end.Y.Bits))
                ),
                new Vec2Fixed(
                    new Fixed(Math.Max(start.X.Bits, end.X.Bits)), 
                    new Fixed(Math.Max(start.Y.Bits, end.Y.Bits))
                )
            );
        }

        private static SegmentDirection CalculateDirection(Vec2Fixed delta)
        {
            if (MathHelper.IsZero(delta.X))
                return SegmentDirection.Vertical;
            if (MathHelper.IsZero(delta.Y))
                return SegmentDirection.Horizontal;
            return MathHelper.DifferentSign(delta.X, delta.Y) ? SegmentDirection.NegativeSlope : SegmentDirection.PositiveSlope;
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
        public static Rotation Rotation(Vec2Fixed first, Vec2Fixed second, Vec2Fixed third)
        {
            return new Seg2FixedBase(first, second).ToSide(third);
        }

        /// <summary>
        /// Gets the time the point would have on the segment. This does not
        /// need to be between the [0, 1] range.
        /// </summary>
        /// <remarks>
        /// If the point is not on the segment, then the result will be wrong.
        /// A corollary to this is that <code>Start + t*Delta = point</code>.
        /// </remarks>
        /// <param name="point">The point to get the time for.</param>
        /// <returns>The time the point is on this segment.</returns>
        public Fixed ToTime(Vec2Fixed point)
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
        public bool Overlaps(Seg2FixedBase seg)
        {
            Fixed tStart = ToTime(seg.Start);
            Fixed tEnd = ToTime(seg.End);
            return (tStart > 0 && tStart < 1) || (tEnd > 0 && tEnd < 1);
        }

        /// <summary>
        /// Checks if the box intersects this segment.
        /// </summary>
        /// <param name="box">The box to check.</param>
        /// <returns>True if it intersects, false if not.</returns>
        public bool Intersects(Box2Fixed box)
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

        /// <summary>
        /// Checks if the segments are collinear to each other.
        /// </summary>
        /// <param name="seg">The segment to check.</param>
        /// <param name="epsilon">The optional epsilon for comparisons.</param>
        /// <returns>True if collinear, false if not.</returns>
        public bool Collinear(Seg2FixedBase seg)
        {
            // If the midpoint of the provided segment is on the current segment
            // line, it's reasonably collinear.
            Vec2Fixed midpoint = (seg.Start + seg.End) / 2;
            Vec2Fixed expectedMidpoint = FromTime(ToTime(midpoint));
            return midpoint.EqualTo(expectedMidpoint);
        }

        /// <summary>
        /// Gets the right rotated normal with respect to the direction of this
        /// segment.
        /// </summary>
        /// <returns>The 90 degree right angle rotation of this line, and is
        /// normalized.</returns>
        public Vec2Fixed RightRotateNormal() => new Vec2Fixed(Delta.Y, -Delta.X).Unit();
    }
}
