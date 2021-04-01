using System;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments.Enums;
using Helion.Geometry.Vectors;
using Helion.Util.Extensions;

namespace Helion.Geometry.SegmentsNew
{
    public struct Seg2D
    {
        public Vec2D Start;
        public Vec2D End;

        public Vec2D Delta => End - Start;
        public Box2D Box => new((Start.X.Min(End.X), Start.Y.Min(End.Y)), (Start.X.Max(End.X), Start.Y.Max(End.Y)));
        public SegmentDirection Direction => CalculateDirection();

        public Seg2D(Vec2D start, Vec2D end)
        {
            Start = start;
            End = end;
        }
        
        public static implicit operator Seg2D(ValueTuple<Vec2D, Vec2D> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }
        
        public static Seg2D operator +(Seg2D self, Vec2D other) => new(self.Start + other, self.End + other);
        public static Seg2D operator +(Seg2D self, Vector2D other) => new(self.Start + other, self.End + other);
        public static Seg2D operator -(Seg2D self, Vec2D other) => new(self.Start - other, self.End - other);
        public static Seg2D operator -(Seg2D self, Vector2D other) => new(self.Start - other, self.End - other);
        public static bool operator ==(Seg2D self, Seg2D other) => self.Start == other.Start && self.End == other.End;
        public static bool operator !=(Seg2D self, Seg2D other) => !(self == other);
        
        public void Deconstruct(out Vec2D start, out Vec2D end)
        {
            start = Start;
            end = End;
        }
        
        public Vec2D this[Endpoint endpoint] => endpoint == Endpoint.Start ? Start : End;
        
        public Vec2D Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;

        public Seg2D WithStart(Vec2D start) => (start, End);
        
        public Seg2D WithEnd(Vec2D end) => (Start, end);
        
        public double Length() => Delta.Length();
        
        public Vec2D RightNormal() => Delta.RotateRight90();
        
        public Vec2D FromTime(double t) => Start + (Delta * t);
        
        public bool SameDirection(Seg2D seg) => SameDirection(seg.Delta);
        
        public bool SameDirection(Vec2D delta)
        {
            Vec2D thisDelta = Delta;
            return !thisDelta.X.DifferentSign(delta.X) && !thisDelta.Y.DifferentSign(delta.Y);
        }
        
        public double PerpDot(in Vec2D point)
        {
            return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));
        }
        
        public double PerpDot(in Vec3D point)
        {
            return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));
        }
        
        public Rotation ToSide(Vec2D point, double epsilon = 0.000001)
        {
            double value = PerpDot(point);
            bool approxZero = value.ApproxZero(epsilon);
            return approxZero ? Rotation.On : (value < 0 ? Rotation.Right : Rotation.Left);
        }
        
        public bool OnRight(in Vec2D point) => PerpDot(point) <= 0;
        
        public bool OnRight(in Vec3D point) => PerpDot(point) <= 0;
        
        public bool OnRight(Seg2D seg) => OnRight(seg.Start) && OnRight(seg.End);
        
        public bool OnRight(Box2D box) => OnRight(box.Min);
        
        public bool DifferentSides(Vec2D first, Vec2D second) => OnRight(first) != OnRight(second);
        
        public bool DifferentSides(Seg2D seg) => OnRight(seg.Start) != OnRight(seg.End);
        
        public bool Parallel(Seg2D seg, double epsilon = 0.000001)
        {
            // If both slopes are the same for seg 1 and 2, then we know the
            // slopes are the same, meaning: d1y / d1x = d2y / d2x. Therefore
            // d1y * d2x == d2y * d1x. This also avoids weird division by zero
            // errors and all that fun stuff from any vertical lines.
            return (Delta.Y * seg.Delta.X).ApproxEquals(Delta.X * seg.Delta.Y, epsilon);
        }
        
        public bool Collinear(Seg2D seg)
        {
            return CollinearHelper(seg.Start, Start, End) && CollinearHelper(seg.End, Start, End);
        }
        
        public bool Intersects(Seg2D other) => Intersection(other, out double t) && (t >= 0 && t <= 1);
        
        public bool Intersection(Seg2D seg, out double t)
        {
            double areaStart = DoubleTriArea(Start, End, seg.End);
            double areaEnd = DoubleTriArea(Start, End, seg.Start);

            if (areaStart.DifferentSign(areaEnd))
            {
                double areaThisStart = DoubleTriArea(seg.Start, seg.End, Start);
                double areaThisEnd = DoubleTriArea(seg.Start, seg.End, End);

                if (areaStart.DifferentSign(areaEnd))
                {
                    t = areaThisStart / (areaThisStart - areaThisEnd);
                    return t >= 0.0 && t <= 1.0;
                }
            }

            t = default;
            return false;
        }
        
        public bool IntersectionAsLine(Seg2D seg, out double tThis)
        {
            double determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
            if (determinant.ApproxZero())
            {
                tThis = default;
                return false;
            }

            Vec2D startDelta = Start - seg.Start;
            tThis = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) / determinant;
            return true;
        }

        public bool IntersectionAsLine(Seg2D seg, out double tThis, out double tOther)
        {
            double determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
            if (determinant.ApproxZero())
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

        public Vec2D ClosestPoint(Vec2D point)
        {
            // Source: https://math.stackexchange.com/questions/2193720/find-a-point-on-a-line-segment-which-is-the-closest-to-other-point-not-on-the-li
            Vec2D pointToStartDelta = Start - point;
            double t = -Delta.Dot(pointToStartDelta) / Delta.Dot(Delta);

            if (t <= 0)
                return Start;
            if (t >= 1)
                return End;
            return FromTime(t);
        }

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
        
        public override string ToString() => $"({Start}), ({End})";
        public override bool Equals(object? obj) => obj is Seg2D seg && Start == seg.Start && End == seg.End;
        public override int GetHashCode() => HashCode.Combine(Start.GetHashCode(), End.GetHashCode());

        private SegmentDirection CalculateDirection()
        {
            Vec2D delta = Delta;
            if (delta.X.ApproxZero())
                return SegmentDirection.Vertical;
            if (delta.Y.ApproxZero())
                return SegmentDirection.Horizontal;
            return delta.X.DifferentSign(delta.Y) ? SegmentDirection.NegativeSlope : SegmentDirection.PositiveSlope;
        }

        private static bool CollinearHelper(in Vec2D first, in Vec2D second, in Vec2D third)
        {
            double determinant = (first.X * (second.Y - third.Y)) + 
                                 (second.X * (third.Y - first.Y)) + 
                                 (third.X * (first.Y - second.Y));
            return determinant.ApproxZero();
        }
        
        private static double DoubleTriArea(Vec2D first, Vec2D second, Vec2D third)
        {
            return ((first.X - third.X) * (second.Y - third.Y)) - ((first.Y - third.Y) * (second.X - third.X));
        }
    }
}
