using System;
using System.Numerics;

namespace Helion.Util.Geometry
{
    public class Seg2fBase
    {
        public readonly Vector2 Start;
        public readonly Vector2 End;
        public readonly Vector2 Delta;

        public Seg2fBase(Vector2 start, Vector2 end)
        {
            Assert.Precondition(start != end, "Segment should not be a point");

            Start = start;
            End = end;
            Delta = end - start;
        }

        public static float DoubleTriArea(Vector2 a, Vector2 b, Vector2 c)
        {
            return ((a.X - c.X) * (b.Y - c.Y)) - ((a.Y - c.Y) * (b.X - c.X));
        }

        public Vector2 this[int index] => index == 0 ? Start : End;
        public Vector2 this[Endpoint endpoint] => endpoint == Endpoint.Start ? Start : End;
        public Vector2 Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;
        public Vector2 FromTime(float t) => Start + (Delta * t);

        public bool SameDirection(Seg2fBase seg) => SameDirection(seg.Delta);

        public bool SameDirection(Vector2 delta)
        {
            return !MathHelper.DifferentSign(Delta.X, delta.X) && !MathHelper.DifferentSign(Delta.Y, delta.Y);
        }

        public float PerpDot(Vector2 point)
        {
            return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));
        }

        public SegmentSide ToSide(Vector2 point, float epsilon = 0.00001f)
        {
            float value = PerpDot(point);
            bool approxZero = MathHelper.IsZero(value, epsilon);
            return approxZero ? SegmentSide.On : (value < 0 ? SegmentSide.Right : SegmentSide.Left);
        }

        public bool OnRight(Vector2 point) => PerpDot(point) <= 0;
        public bool OnRight(Seg2fBase seg) => OnRight(seg.Start) && OnRight(seg.End);

        public bool OnRight(Box2f box)
        {
            return OnRight(box.BottomLeft) && 
                   OnRight(box.BottomRight) &&
                   OnRight(box.TopLeft) &&
                   OnRight(box.TopRight);
        }

        public bool DifferentSides(Vector2 first, Vector2 second) => OnRight(first) != OnRight(second);
        public bool DifferentSides(Seg2fBase seg) => OnRight(seg.Start) != OnRight(seg.End);

        public bool Parallel(Seg2fBase seg, float epsilon = 0.00001f)
        {
            // If both slopes are the same for seg 1 and 2, then we know the
            // slopes are the same, meaning: d1y / d1x = d2y / d2x. Therefore
            // d1y * d2x == d2y * d1x. This also avoids weird division by zero
            // errors and all that fun stuff from any vertical lines.
            return MathHelper.AreEqual(Delta.Y * seg.Delta.X, Delta.X * seg.Delta.Y, epsilon);
        }

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

        public bool Intersects(Seg2fBase other) => Intersection(other, out float t) ? (0 <= t && t <= 1) : false;

        public bool Intersection(Seg2fBase seg, out float t)
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

        public bool IntersectionAsLine(Seg2fBase seg, out float tThis)
        {
            float determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
            if (MathHelper.IsZero(determinant))
            {
                tThis = default;
                return false;
            }

            Vector2 startDelta = Start - seg.Start;
            float inverseDeterminant = 1.0f / determinant;
            tThis = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) * inverseDeterminant;
            return true;
        }

        public bool IntersectionAsLine(Seg2fBase seg, out float tThis, out float tOther)
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

        public float Length() => Delta.Length();

        public float LengthSquared() => Delta.LengthSquared();
    }

    public class Seg2f : Seg2fBase
    {
        public readonly Vector2 DeltaInverse;
        public readonly Box2f Box;
        public readonly SegmentDirection Direction;

        public Seg2f(Vector2 start, Vector2 end) : base(start, end)
        {
            DeltaInverse = new Vector2(1.0f / Delta.X, 1.0f / Delta.Y);
            Box = MakeBox(start, end);
            Direction = CalculateDirection(Delta);
        }

        private static Box2f MakeBox(Vector2 start, Vector2 end)
        {
            return new Box2f(
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

        public static SegmentSide Rotation(Vector2 first, Vector2 second, Vector2 third, float epsilon = 0.00001f)
        {
            return new Seg2fBase(first, second).ToSide(third, epsilon);
        }

        public float ToTime(Vector2 point)
        {
            if (!MathHelper.IsZero(Delta.X))
                return (point.X - Start.X) * DeltaInverse.X;
            return (point.Y - Start.Y) * DeltaInverse.Y;
        }

        public bool Overlaps(Seg2fBase seg)
        {
            float tStart = ToTime(seg.Start);
            float tEnd = ToTime(seg.End);
            return (tStart > 0 && tStart < 1) || (tEnd > 0 && tEnd < 1);
        }

        public bool Intersects(Box2f box)
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

        public bool Collinear(Seg2fBase seg, float epsilon = 0.0001f)
        {
            // If the midpoint of the provided segment is on the current segment
            // line, it's reasonably collinear.
            Vector2 midpoint = (seg.Start + seg.End) / 2;
            Vector2 expectedMidpoint = FromTime(ToTime(midpoint));
            return midpoint.EqualTo(expectedMidpoint, epsilon);
        }

        public Tuple<Seg2f, Seg2f> Split(float t)
        {
            Assert.Precondition(t > 0 && t < 1, $"Cannot split segment outside the line or at endpoints: {t}");

            Vector2 middle = FromTime(t);
            return Tuple.Create(new Seg2f(Start, middle), new Seg2f(middle, End));
        }

        public Vector2 RightRotateNormal() => new Vector2(Delta.Y, -Delta.X).Unit();
    }
}
