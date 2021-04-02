// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Util.Extensions;

namespace Helion.Geometry.Segments
{
    public class SegmentT2F
    {
        public T Start;
        public T End;

        public Vec2F Delta => End - Start;
        public float Length => Start.Distance(End);
        public bool IsAxisAligned => Start.X.ApproxEquals(End.X) || Start.Y.ApproxEquals(End.Y);
        public Box2F Box => new((Start.X.Min(End.X), Start.Y.Min(End.Y)), (Start.X.Max(End.X), Start.Y.Max(End.Y)));
        public IEnumerable<T> Vertices => GetVertices();

        public SegmentT2F(T start, T end)
        {
            Start = start;
            End = end;
        }

        public static implicit operator SegmentT2F(ValueTuple<T, T> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public void Deconstruct(out T start, out T end)
        {
            start = Start;
            end = End;
        }

        public T this[int index] => index == 0 ? Start : End;
        public T this[Endpoint endpoint] => endpoint == Endpoint.Start ? Start : End;

        public static Seg2F operator +(SegmentT2F self, Vec2F other) => new(self.Start + other, self.End + other);
        public static Seg2F operator +(SegmentT2F self, T other) => new(self.Start + other, self.End + other);
        public static Seg2F operator -(SegmentT2F self, Vec2F other) => new(self.Start - other, self.End - other);
        public static Seg2F operator -(SegmentT2F self, T other) => new(self.Start - other, self.End - other);
        public static bool operator ==(SegmentT2F self, SegmentT2F other) => self.Start == other.Start && self.End == other.End;
        public static bool operator !=(SegmentT2F self, SegmentT2F other) => !(self == other);

        public Vec2F Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;
        public Seg2F WithStart(Vec2F start) => (start, End);
        public Seg2F WithStart(Vector2F start) => (start.Struct, End);
        public Seg2F WithEnd(Vec2F end) => (Start, end);
        public Seg2F WithEnd(Vector2F end) => (Start, end.Struct);
        public Vec2F FromTime(float t) => Start + (Delta * t);
        public bool SameDirection(Seg2F seg) => SameDirection(seg.Delta);
        public bool SameDirection(Segment2F seg) => SameDirection(seg.Delta);
        public bool SameDirection(Vec2F delta)
        {
            Vec2F thisDelta = Delta;
            return !thisDelta.X.DifferentSign(delta.X) && !thisDelta.Y.DifferentSign(delta.Y);
        }
        public bool SameDirection(Vector2F delta)
        {
            Vec2F thisDelta = Delta;
            return !thisDelta.X.DifferentSign(delta.X) && !thisDelta.Y.DifferentSign(delta.Y);
        }
        public double PerpDot(Vec2F point)
        {
            return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));
        }
        public double PerpDot(Vector2F point)
        {
            return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));
        }
        public double PerpDot(Vec3F point) => PerpDot(point.XY);
        public double PerpDot(Vector3F point) => PerpDot(point.XY);
        public bool OnRight(Vec2F point) => PerpDot(point) <= 0;
        public bool OnRight(Vector2F point) => PerpDot(point) <= 0;
        public bool OnRight(Vec3F point) => PerpDot(point.XY) <= 0;
        public bool OnRight(Vector3F point) => PerpDot(point.XY) <= 0;
        public bool DifferentSides(Vec2F first, Vec2F second) => OnRight(first) != OnRight(second);
        public bool DifferentSides(Vector2F first, Vector2F second) => OnRight(first) != OnRight(second);
        public bool DifferentSides(Seg2F seg) => OnRight(seg.Start) != OnRight(seg.End);
        public bool DifferentSides(Segment2F seg) => OnRight(seg.Start) != OnRight(seg.End);
        public Rotation ToSide(Vec2F point, float epsilon = 0.0001f)
        {
            float value = PerpDot(point);
            bool approxZero = value.ApproxZero(epsilon);
            return approxZero ? Rotation.On : (value < 0 ? Rotation.Right : Rotation.Left);
        }
        public bool Parallel(Seg2F seg, float epsilon = 0.0001f)
        {
            return (Delta.Y * seg.Delta.X).ApproxEquals(Delta.X * seg.Delta.Y, epsilon);
        }
        public bool Parallel(Segment2F seg, float epsilon = 0.0001f)
        {
            return (Delta.Y * seg.Delta.X).ApproxEquals(Delta.X * seg.Delta.Y, epsilon);
        }
        public bool Collinear(Seg2F seg)
        {
            return CollinearHelper(seg.Start, Start, End) && CollinearHelper(seg.End, Start, End);
        }
        public bool Collinear(Segment2F seg)
        {
            return CollinearHelper(seg.Start, Start, End) && CollinearHelper(seg.End, Start, End);
        }
        public bool Intersects(Seg2F other) => Intersection(other, out float t) && (t >= 0 && t <= 1);
        public bool Intersects(Segment2F other) => Intersection(other, out float t) && (t >= 0 && t <= 1);
        public bool Intersection(Seg2F seg, out float t)
        {
            float areaStart = DoubleTriArea(Start, End, seg.End);
            float areaEnd = DoubleTriArea(Start, End, seg.Start);

            if (areaStart.DifferentSign(areaEnd))
            {
                float areaThisStart = DoubleTriArea(seg.Start, seg.End, Start);
                float areaThisEnd = DoubleTriArea(seg.Start, seg.End, End);
                
                if (areaStart.DifferentSign(areaEnd))
                {
                    t = areaThisStart / (areaThisStart - areaThisEnd);
                    return t >= 0 && t <= 1;
                }
            }

            t = default;
            return false;
        }
        public bool Intersection(Segment2F seg, out float t)
        {
            float areaStart = DoubleTriArea(Start, End, seg.End);
            float areaEnd = DoubleTriArea(Start, End, seg.Start);

            if (areaStart.DifferentSign(areaEnd))
            {
                float areaThisStart = DoubleTriArea(seg.Start, seg.End, Start);
                float areaThisEnd = DoubleTriArea(seg.Start, seg.End, End);
                
                if (areaStart.DifferentSign(areaEnd))
                {
                    t = areaThisStart / (areaThisStart - areaThisEnd);
                    return t >= 0 && t <= 1;
                }
            }

            t = default;
            return false;
        }
        public bool IntersectionAsLine(Seg2F seg, out float tThis)
        {
            float determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
            if (determinant.ApproxZero())
            {
                tThis = default;
                return false;
            }

            Vec2F startDelta = Start - seg.Start;
            tThis = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) / determinant;
            return true;
        }
        public bool IntersectionAsLine(Segment2F seg, out float tThis)
        {
            float determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
            if (determinant.ApproxZero())
            {
                tThis = default;
                return false;
            }

            Vec2F startDelta = Start - seg.Start;
            tThis = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) / determinant;
            return true;
        }
        public bool IntersectionAsLine(Seg2F seg, out float tThis, out float tOther)
        {
            float determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
            if (determinant.ApproxZero())
            {
                    tThis = default;
                    tOther = default;
                    return false;
            }

            Vec2F startDelta = Start - seg.Start;
            float inverseDeterminant = 1.0f / determinant;
            tThis = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) * inverseDeterminant;
            tOther = ((-Delta.Y * startDelta.X) + (Delta.X * startDelta.Y)) * inverseDeterminant;
            return true;
        }
        public bool IntersectionAsLine(Segment2F seg, out float tThis, out float tOther)
        {
            float determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
            if (determinant.ApproxZero())
            {
                    tThis = default;
                    tOther = default;
                    return false;
            }

            Vec2F startDelta = Start - seg.Start;
            float inverseDeterminant = 1.0f / determinant;
            tThis = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) * inverseDeterminant;
            tOther = ((-Delta.Y * startDelta.X) + (Delta.X * startDelta.Y)) * inverseDeterminant;
            return true;
        }
        public Vec2F ClosestPoint(Vec2F point)
        {
            Vec2F pointToStartDelta = Start - point;
            float t = -Delta.Dot(pointToStartDelta) / Delta.Dot(Delta);

            if (t <= 0)
                return Start;
            if (t >= 1)
                return End;
            return FromTime(t);
        }
        public Vec2F ClosestPoint(Vector2F point)
        {
            Vec2F pointToStartDelta = Start - point;
            float t = -Delta.Dot(pointToStartDelta) / Delta.Dot(Delta);

            if (t <= 0)
                return Start;
            if (t >= 1)
                return End;
            return FromTime(t);
        }
        public bool Intersects(Box2F box)
        {
            if (Start.X.ApproxEquals(End.X))
                return box.Min.X < Start.X && Start.X < box.Max.X;
            if (Start.Y.ApproxEquals(End.Y))
                return box.Min.Y < Start.Y && Start.Y < box.Max.Y;
            return (box.Min.X < box.Max.X) ^ (box.Min.Y < box.Max.Y) ?
                DifferentSides(box.BottomLeft, box.TopRight) :
                DifferentSides(box.TopLeft, box.BottomRight);
        }

        public override string ToString() => $"({Start}), ({End})";
        public override bool Equals(object? obj) => obj is SegmentT2F seg && Start == seg.Start && End == seg.End;
        public override int GetHashCode() => HashCode.Combine(Start.GetHashCode(), End.GetHashCode());

        private IEnumerable<T> GetVertices()
        {
            yield return Start;
            yield return End;
        }
        private static bool CollinearHelper(Vec2F first, Vec2F second, Vec2F third)
        {
            return ((first.X * (second.Y - third.Y)) + (second.X * (third.Y - first.Y)) + (third.X * (first.Y - second.Y))).ApproxZero();
        }
        private static double DoubleTriArea(Vec2F first, Vec2F second, Vec2F third)
        {
            return ((first.X - third.X) * (second.Y - third.Y)) - ((first.Y - third.Y) * (second.X - third.X));
        }
    }
}
