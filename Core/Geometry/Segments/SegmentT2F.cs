// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Util.Extensions;

namespace Helion.Geometry.Segments
{
    public class SegmentT2F<V> where V : Vector2F
    {
        public readonly V Start;
        public readonly V End;
        public readonly Vec2F Delta;
        public readonly Box2F Box;

        public float Length => Start.Distance(End);
        public Seg2F Struct => new(Start, End);
        public bool IsAxisAligned => Start.X.ApproxEquals(End.X) || Start.Y.ApproxEquals(End.Y);
        public IEnumerable<V> Vertices => GetVertices();

        public SegmentT2F(V start, V end)
        {
            Start = start;
            End = end;
            Delta = End - Start;
            Box = new((Start.X.Min(End.X), Start.Y.Min(End.Y)), (Start.X.Max(End.X), Start.Y.Max(End.Y)));
        }

        public void Deconstruct(out V start, out V end)
        {
            start = Start;
            end = End;
        }

        public V this[int index] => index == 0 ? Start : End;
        public V this[Endpoint endpoint] => endpoint == Endpoint.Start ? Start : End;

        public static Seg2F operator +(SegmentT2F<V> self, Vec2F other) => new(self.Start + other, self.End + other);
        public static Seg2F operator +(SegmentT2F<V> self, Vector2F other) => new(self.Start + other, self.End + other);
        public static Seg2F operator -(SegmentT2F<V> self, Vec2F other) => new(self.Start - other, self.End - other);
        public static Seg2F operator -(SegmentT2F<V> self, Vector2F other) => new(self.Start - other, self.End - other);

        public V Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;
        public Vec2F FromTime(float t) => Start + (Delta * t);
        public bool SameDirection(Seg2F seg) => SameDirection(seg.Delta);
        public bool SameDirection(Segment2F seg) => SameDirection(seg.Delta);
        public bool SameDirection<T>(SegmentT2F<T> seg) where T : Vector2F => SameDirection(seg.Delta);
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
        public float PerpDot(Vec2F point)
        {
            return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));
        }
        public float PerpDot(Vector2F point)
        {
            return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));
        }
        public double PerpDot(Vec3F point) => PerpDot(point.XY);
        public double PerpDot(Vector3F point) => PerpDot(point.XY);
        public bool OnRight(Vec2F point) => PerpDot(point) <= 0;
        public bool OnRight(Vector2F point) => PerpDot(point) <= 0;
        public bool OnRight(Vec3F point) => PerpDot(point.XY) <= 0;
        public bool OnRight(Vector3F point) => PerpDot(point.XY) <= 0;
        public bool OnRight(Seg2F seg) => OnRight(seg.Start) && OnRight(seg.End);
        public bool OnRight(Segment2F seg) => OnRight(seg.Start) && OnRight(seg.End);
        public bool OnRight<T>(SegmentT2F<T> seg) where T : Vector2F => OnRight(seg.Start) && OnRight(seg.End);
        public bool DifferentSides(Vec2F first, Vec2F second) => OnRight(first) != OnRight(second);
        public bool DifferentSides(Vector2F first, Vector2F second) => OnRight(first) != OnRight(second);
        public bool DifferentSides(Seg2F seg) => OnRight(seg.Start) != OnRight(seg.End);
        public bool DifferentSides(Segment2F seg) => OnRight(seg.Start) != OnRight(seg.End);
        public bool DifferentSides<T>(SegmentT2F<T> seg) where T : Vector2F => OnRight(seg.Start) != OnRight(seg.End);
        public Rotation ToSide(Vec2F point, float epsilon = 0.0001f)
        {
            float value = PerpDot(point);
            bool approxZero = value.ApproxZero(epsilon);
            return approxZero ? Rotation.On : (value < 0 ? Rotation.Right : Rotation.Left);
        }
        public Rotation ToSide(Vector2F point, float epsilon = 0.0001f)
        {
            float value = PerpDot(point);
            bool approxZero = value.ApproxZero(epsilon);
            return approxZero ? Rotation.On : (value < 0 ? Rotation.Right : Rotation.Left);
        }
        public float ToTime(Vec2F point)
        {
            if (Start.X.ApproxEquals(End.X))
                return (point.Y - Start.Y) / (End.Y - Start.Y);
            return (point.X - Start.X) / (End.X - Start.X);
        }
        public float ToTime(Vector2F point)
        {
            if (Start.X.ApproxEquals(End.X))
                return (point.Y - Start.Y) / (End.Y - Start.Y);
            return (point.X - Start.X) / (End.X - Start.X);
        }
        public bool Parallel(Seg2F seg, float epsilon = 0.0001f)
        {
            return (Delta.Y * seg.Delta.X).ApproxEquals(Delta.X * seg.Delta.Y, epsilon);
        }
        public bool Parallel(Segment2F seg, float epsilon = 0.0001f)
        {
            return (Delta.Y * seg.Delta.X).ApproxEquals(Delta.X * seg.Delta.Y, epsilon);
        }
        public bool Parallel<T>(SegmentT2F<T> seg, float epsilon = 0.0001f) where T : Vector2F
        {
            return (Delta.Y * seg.Delta.X).ApproxEquals(Delta.X * seg.Delta.Y, epsilon);
        }
        public bool Collinear(Seg2F seg)
        {
            return CollinearHelper(seg.Start.X, seg.Start.Y, Start.X, Start.Y, End.X, End.Y) &&
                   CollinearHelper(seg.End.X, seg.End.Y, Start.X, Start.Y, End.X, End.Y);
        }
        public bool Collinear(Segment2F seg)
        {
            return CollinearHelper(seg.Start.X, seg.Start.Y, Start.X, Start.Y, End.X, End.Y) &&
                   CollinearHelper(seg.End.X, seg.End.Y, Start.X, Start.Y, End.X, End.Y);
        }
        public bool Collinear<T>(SegmentT2F<T> seg) where T : Vector2F
        {
            return CollinearHelper(seg.Start.X, seg.Start.Y, Start.X, Start.Y, End.X, End.Y) &&
                   CollinearHelper(seg.End.X, seg.End.Y, Start.X, Start.Y, End.X, End.Y);
        }
        public bool Intersects(Seg2F other) => Intersection(other, out float t) && (t >= 0 && t <= 1);
        public bool Intersects(Segment2F other) => Intersection(other, out float t) && (t >= 0 && t <= 1);
        public bool Intersects<T>(SegmentT2F<T> other) where T : Vector2F => Intersection(other, out float t) && (t >= 0 && t <= 1);
        public bool Intersection(Seg2F seg, out float t)
        {
            float areaStart = DoubleTriArea(Start.X, Start.Y, End.X, End.Y, seg.End.X, seg.End.Y);
            float areaEnd = DoubleTriArea(Start.X, Start.Y, End.X, End.Y, seg.Start.X, seg.Start.Y);

            if (areaStart.DifferentSign(areaEnd))
            {
                float areaThisStart = DoubleTriArea(seg.Start.X, seg.Start.Y, seg.End.X, seg.End.Y, Start.X, Start.Y);
                float areaThisEnd = DoubleTriArea(seg.Start.X, seg.Start.Y, seg.End.X, seg.End.Y, End.X, End.Y);
                
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
            float areaStart = DoubleTriArea(Start.X, Start.Y, End.X, End.Y, seg.End.X, seg.End.Y);
            float areaEnd = DoubleTriArea(Start.X, Start.Y, End.X, End.Y, seg.Start.X, seg.Start.Y);

            if (areaStart.DifferentSign(areaEnd))
            {
                float areaThisStart = DoubleTriArea(seg.Start.X, seg.Start.Y, seg.End.X, seg.End.Y, Start.X, Start.Y);
                float areaThisEnd = DoubleTriArea(seg.Start.X, seg.Start.Y, seg.End.X, seg.End.Y, End.X, End.Y);
                
                if (areaStart.DifferentSign(areaEnd))
                {
                    t = areaThisStart / (areaThisStart - areaThisEnd);
                    return t >= 0 && t <= 1;
                }
            }

            t = default;
            return false;
        }
        public bool Intersection<T>(SegmentT2F<T> seg, out float t) where T : Vector2F
        {
            float areaStart = DoubleTriArea(Start.X, Start.Y, End.X, End.Y, seg.End.X, seg.End.Y);
            float areaEnd = DoubleTriArea(Start.X, Start.Y, End.X, End.Y, seg.Start.X, seg.Start.Y);

            if (areaStart.DifferentSign(areaEnd))
            {
                float areaThisStart = DoubleTriArea(seg.Start.X, seg.Start.Y, seg.End.X, seg.End.Y, Start.X, Start.Y);
                float areaThisEnd = DoubleTriArea(seg.Start.X, seg.Start.Y, seg.End.X, seg.End.Y, End.X, End.Y);
                
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
        public bool IntersectionAsLine<T>(SegmentT2F<T> seg, out float tThis) where T : Vector2F
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
        public bool IntersectionAsLine<T>(SegmentT2F<T> seg, out float tThis, out float tOther) where T : Vector2F
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
                return Start.Struct;
            if (t >= 1)
                return End.Struct;
            return FromTime(t);
        }
        public Vec2F ClosestPoint(Vector2F point)
        {
            Vec2F pointToStartDelta = Start - point;
            float t = -Delta.Dot(pointToStartDelta) / Delta.Dot(Delta);

            if (t <= 0)
                return Start.Struct;
            if (t >= 1)
                return End.Struct;
            return FromTime(t);
        }
        public bool Intersects(Box2F box)
        {
            if (!box.Overlaps(Box))
                return false;
            if (Start.X.ApproxEquals(End.X))
                return box.Min.X < Start.X && Start.X < box.Max.X;
            if (Start.Y.ApproxEquals(End.Y))
                return box.Min.Y < Start.Y && Start.Y < box.Max.Y;
            return ((Start.X < End.X) ^ (Start.Y < End.Y)) ? 
                DifferentSides(box.BottomLeft, box.TopRight) :
                DifferentSides(box.TopLeft, box.BottomRight);
        }
        public bool Intersects(BoundingBox2F box)
        {
            if (!box.Overlaps(Box))
                return false;
            if (Start.X.ApproxEquals(End.X))
                return box.Min.X < Start.X && Start.X < box.Max.X;
            if (Start.Y.ApproxEquals(End.Y))
                return box.Min.Y < Start.Y && Start.Y < box.Max.Y;
            return ((Start.X < End.X) ^ (Start.Y < End.Y)) ? 
                DifferentSides(box.BottomLeft, box.TopRight) :
                DifferentSides(box.TopLeft, box.BottomRight);
        }

        private static bool CollinearHelper(float aX, float aY, float bX, float bY, float cX, float cY)
        {
            return ((aX * (bY - cY)) + (bX * (cY - aY)) + (cX * (aY - bY))).ApproxZero();
        }
        private static float DoubleTriArea(float aX, float aY, float bX, float bY, float cX, float cY)
        {
            return ((aX - cX) * (bY - cY)) - ((aY - cY) * (bX - cX));
        }
        public override string ToString() => $"({Start}), ({End})";
        public override bool Equals(object? obj) => obj is SegmentT2F<V> seg && Start == seg.Start && End == seg.End;
        public override int GetHashCode() => HashCode.Combine(Start.GetHashCode(), End.GetHashCode());

        private IEnumerable<V> GetVertices()
        {
            yield return Start;
            yield return End;
        }
    }
}
