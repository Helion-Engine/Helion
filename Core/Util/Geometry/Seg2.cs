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

        public Vector2 this[int index] => index == 0 ? Start : End;
        public Vector2 this[Endpoint endpoint] => endpoint == Endpoint.Start ? Start : End;
        public Vector2 Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;
        public Vector2 FromTime(float t) => Start + (Delta * t);

        public bool SameDirection(Seg2fBase seg)
        {
            return SameDirection(seg.Delta);
        }

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
            return approxZero ? SegmentSide.On : value < 0 ? SegmentSide.Right : SegmentSide.Left;
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

        public float Length()
        {
            return Delta.Length();
        }

        public float LengthSquared()
        {
            return Delta.LengthSquared();
        }
    }

    public class Seg2f : Seg2fBase
    {
        public readonly Vector2 DeltaInverse;
        public readonly BBox2f BBox;
        public readonly SegmentDirection Direction;

        public Seg2f(Vector2 start, Vector2 end) : base(start, end)
        {
            DeltaInverse = new Vector2(1.0f / Delta.X, 1.0f / Delta.Y);
            BBox = MakeBox(start, end);
            Direction = CalculateDirection(Delta);
        }

        private static BBox2f MakeBox(Vector2 start, Vector2 end)
        {
            return new BBox2f(
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

        public bool Collinear(Seg2fBase seg, float epsilon = 0.0001f)
        {
            // If the midpoint of the provided segment is on the current segment
            // line, it's reasonably collinear.
            Vector2 midpoint = (seg.Start + seg.End) / 2;
            Vector2 expectedMidpoint = FromTime(ToTime(midpoint));
            return midpoint.EqualTo(expectedMidpoint, epsilon);
        }
    }
}
