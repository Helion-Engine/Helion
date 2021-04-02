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
    public class Segment2D<T> where T : Vector2D
    {
        public T Start;
        public T End;

        public Vec2D Delta => End - Start;
        public double Length => Start.Distance(End);
        public bool IsAxisAligned => Start.X.ApproxEquals(End.X) || Start.Y.ApproxEquals(End.Y);
        public Box2D Box => new((Start.X.Min(End.X), Start.Y.Min(End.Y)), (Start.X.Max(End.X), Start.Y.Max(End.Y)));
        public IEnumerable<T> Vertices => GetVertices();

        public Segment2D(T start, T end)
        {
            Start = start;
            End = end;
        }

        public void Deconstruct(out T start, out T end)
        {
            start = Start;
            end = End;
        }

        public T this[int index] => index == 0 ? Start : End;
        public T this[Endpoint endpoint] => endpoint == Endpoint.Start ? Start : End;

        public static Seg2D operator +(Segment2D<T> self, Vec2D other) => new(self.Start + other, self.End + other);
        public static Seg2D operator +(Segment2D<T> self, T other) => new(self.Start + other, self.End + other);
        public static Seg2D operator -(Segment2D<T> self, Vec2D other) => new(self.Start - other, self.End - other);
        public static Seg2D operator -(Segment2D<T> self, T other) => new(self.Start - other, self.End - other);
        public static bool operator ==(Segment2D<T> self, Segment2D<T> other) => self.Start == other.Start && self.End == other.End;
        public static bool operator !=(Segment2D<T> self, Segment2D<T> other) => !(self == other);

        public T Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;
        public Vec2D FromTime(double t) => Start + (Delta * t);
        public bool SameDirection(Seg2D seg) => SameDirection(seg.Delta);
        public bool SameDirection<T>(Segment2D<T> seg) where T : Vector2D => SameDirection(seg.Delta);
        public bool SameDirection(Vec2D delta)
        {
            Vec2D thisDelta = Delta;
            return !thisDelta.X.DifferentSign(delta.X) && !thisDelta.Y.DifferentSign(delta.Y);
        }
        public bool SameDirection(Vector2D delta)
        {
            Vec2D thisDelta = Delta;
            return !thisDelta.X.DifferentSign(delta.X) && !thisDelta.Y.DifferentSign(delta.Y);
        }
        public double PerpDot(Vec2D point)
        {
            return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));
        }
        public double PerpDot(Vector2D point)
        {
            return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));
        }
        public double PerpDot(Vec3D point) => PerpDot(point.XY);
        public double PerpDot(Vector3D point) => PerpDot(point.XY);
        public bool OnRight(Vec2D point) => PerpDot(point) <= 0;
        public bool OnRight(Vector2D point) => PerpDot(point) <= 0;
        public bool OnRight(Vec3D point) => PerpDot(point.XY) <= 0;
        public bool OnRight(Vector3D point) => PerpDot(point.XY) <= 0;
        public bool DifferentSides(Vec2D first, Vec2D second) => OnRight(first) != OnRight(second);
        public bool DifferentSides(Vector2D first, Vector2D second) => OnRight(first) != OnRight(second);
        public bool DifferentSides(Seg2D seg) => OnRight(seg.Start) != OnRight(seg.End);
        public bool DifferentSides<T>(Segment2D<T> seg) where T : Vector2D => OnRight(seg.Start) != OnRight(seg.End);
        public Rotation ToSide(Vec2D point, double epsilon = 0.000001)
        {
            double value = PerpDot(point);
            bool approxZero = value.ApproxZero(epsilon);
            return approxZero ? Rotation.On : (value < 0 ? Rotation.Right : Rotation.Left);
        }
        public Rotation ToSide(Vector2D point, double epsilon = 0.000001)
        {
            double value = PerpDot(point);
            bool approxZero = value.ApproxZero(epsilon);
            return approxZero ? Rotation.On : (value < 0 ? Rotation.Right : Rotation.Left);
        }
        public bool Parallel(Seg2D seg, double epsilon = 0.000001)
        {
            return (Delta.Y * seg.Delta.X).ApproxEquals(Delta.X * seg.Delta.Y, epsilon);
        }
        public bool Parallel<T>(Segment2D<T> seg, double epsilon = 0.000001) where T : Vector2D
        {
            return (Delta.Y * seg.Delta.X).ApproxEquals(Delta.X * seg.Delta.Y, epsilon);
        }
        public bool Collinear(Seg2D seg)
        {
            return CollinearHelper(seg.Start.X, seg.Start.Y, Start.X, Start.Y, End.X, End.Y) &&
                   CollinearHelper(seg.End.X, seg.End.Y, Start.X, Start.Y, End.X, End.Y);
        }
        public bool Collinear<T>(Segment2D<T> seg) where T : Vector2D
        {
            return CollinearHelper(seg.Start.X, seg.Start.Y, Start.X, Start.Y, End.X, End.Y) &&
                   CollinearHelper(seg.End.X, seg.End.Y, Start.X, Start.Y, End.X, End.Y);
        }
        public bool Intersects(Seg2D other) => Intersection(other, out double t) && (t >= 0 && t <= 1);
        public bool Intersects<T>(Segment2D<T> other) where T : Vector2D => Intersection(other, out double t) && (t >= 0 && t <= 1);
        public bool Intersection(Seg2D seg, out double t)
        {
            double areaStart = DoubleTriArea(Start.X, Start.Y, End.X, End.Y, seg.End.X, seg.End.Y);
            double areaEnd = DoubleTriArea(Start.X, Start.Y, End.X, End.Y, seg.Start.X, seg.Start.Y);

            if (areaStart.DifferentSign(areaEnd))
            {
                double areaThisStart = DoubleTriArea(seg.Start.X, seg.Start.Y, seg.End.X, seg.End.Y, Start.X, Start.Y);
                double areaThisEnd = DoubleTriArea(seg.Start.X, seg.Start.Y, seg.End.X, seg.End.Y, End.X, End.Y);
                
                if (areaStart.DifferentSign(areaEnd))
                {
                    t = areaThisStart / (areaThisStart - areaThisEnd);
                    return t >= 0 && t <= 1;
                }
            }

            t = default;
            return false;
        }
        public bool Intersection<T>(Segment2D<T> seg, out double t) where T : Vector2D
        {
            double areaStart = DoubleTriArea(Start.X, Start.Y, End.X, End.Y, seg.End.X, seg.End.Y);
            double areaEnd = DoubleTriArea(Start.X, Start.Y, End.X, End.Y, seg.Start.X, seg.Start.Y);

            if (areaStart.DifferentSign(areaEnd))
            {
                double areaThisStart = DoubleTriArea(seg.Start.X, seg.Start.Y, seg.End.X, seg.End.Y, Start.X, Start.Y);
                double areaThisEnd = DoubleTriArea(seg.Start.X, seg.Start.Y, seg.End.X, seg.End.Y, End.X, End.Y);
                
                if (areaStart.DifferentSign(areaEnd))
                {
                    t = areaThisStart / (areaThisStart - areaThisEnd);
                    return t >= 0 && t <= 1;
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
        public bool IntersectionAsLine<T>(Segment2D<T> seg, out double tThis) where T : Vector2D
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
        public bool IntersectionAsLine<T>(Segment2D<T> seg, out double tThis, out double tOther) where T : Vector2D
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
            Vec2D pointToStartDelta = Start - point;
            double t = -Delta.Dot(pointToStartDelta) / Delta.Dot(Delta);

            if (t <= 0)
                return Start.Struct;
            if (t >= 1)
                return End.Struct;
            return FromTime(t);
        }
        public Vec2D ClosestPoint(Vector2D point)
        {
            Vec2D pointToStartDelta = Start - point;
            double t = -Delta.Dot(pointToStartDelta) / Delta.Dot(Delta);

            if (t <= 0)
                return Start.Struct;
            if (t >= 1)
                return End.Struct;
            return FromTime(t);
        }
        public bool Intersects(Box2D box)
        {
            if (Start.X.ApproxEquals(End.X))
                return box.Min.X < Start.X && Start.X < box.Max.X;
            if (Start.Y.ApproxEquals(End.Y))
                return box.Min.Y < Start.Y && Start.Y < box.Max.Y;
            return (box.Min.X < box.Max.X) ^ (box.Min.Y < box.Max.Y) ?
                DifferentSides(box.BottomLeft, box.TopRight) :
                DifferentSides(box.TopLeft, box.BottomRight);
        }

        private static bool CollinearHelper(double aX, double aY, double bX, double bY, double cX, double cY)
        {
            return ((aX * (bY - cY)) + (bX * (cY - aY)) + (cX * (aY - bY))).ApproxZero();
        }
        private static double DoubleTriArea(double aX, double aY, double bX, double bY, double cX, double cY)
        {
            return ((aX - cX) * (bY - cY)) - ((aY - cY) * (bX - cX));
        }
        public override string ToString() => $"({Start}), ({End})";
        public override bool Equals(object? obj) => obj is Segment2D<T> seg && Start == seg.Start && End == seg.End;
        public override int GetHashCode() => HashCode.Combine(Start.GetHashCode(), End.GetHashCode());

        private IEnumerable<T> GetVertices()
        {
            yield return Start;
            yield return End;
        }
    }
}
