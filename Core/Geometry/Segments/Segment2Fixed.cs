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
    public class Segment2Fixed
    {
        public Vec2Fixed Start;
        public Vec2Fixed End;

        public Vec2Fixed Delta => End - Start;
        public Fixed Length => Start.Distance(End);
        public bool IsAxisAligned => Start.X.ApproxEquals(End.X) || Start.Y.ApproxEquals(End.Y);
        public Box2Fixed Box => new((Start.X.Min(End.X), Start.Y.Min(End.Y)), (Start.X.Max(End.X), Start.Y.Max(End.Y)));
        public IEnumerable<Vec2Fixed> Vertices => GetVertices();

        public Segment2Fixed(Vec2Fixed start, Vec2Fixed end)
        {
            Start = start;
            End = end;
        }

        public static implicit operator Segment2Fixed(ValueTuple<Vec2Fixed, Vec2Fixed> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public void Deconstruct(out Vec2Fixed start, out Vec2Fixed end)
        {
            start = Start;
            end = End;
        }

        public Vec2Fixed this[int index] => index == 0 ? Start : End;
        public Vec2Fixed this[Endpoint endpoint] => endpoint == Endpoint.Start ? Start : End;

        public static Seg2Fixed operator +(Segment2Fixed self, Vec2Fixed other) => new(self.Start + other, self.End + other);
        public static Seg2Fixed operator +(Segment2Fixed self, Vector2Fixed other) => new(self.Start + other, self.End + other);
        public static Seg2Fixed operator -(Segment2Fixed self, Vec2Fixed other) => new(self.Start - other, self.End - other);
        public static Seg2Fixed operator -(Segment2Fixed self, Vector2Fixed other) => new(self.Start - other, self.End - other);
        public static bool operator ==(Segment2Fixed self, Segment2Fixed other) => self.Start == other.Start && self.End == other.End;
        public static bool operator !=(Segment2Fixed self, Segment2Fixed other) => !(self == other);

        public Vec2Fixed Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;
        public Seg2Fixed WithStart(Vec2Fixed start) => (start, End);
        public Seg2Fixed WithStart(Vector2Fixed start) => (start.Struct, End);
        public Seg2Fixed WithEnd(Vec2Fixed end) => (Start, end);
        public Seg2Fixed WithEnd(Vector2Fixed end) => (Start, end.Struct);
        public Vec2Fixed FromTime(Fixed t) => Start + (Delta * t);
        public bool SameDirection(Seg2Fixed seg) => SameDirection(seg.Delta);
        public bool SameDirection(Segment2Fixed seg) => SameDirection(seg.Delta);
        public bool SameDirection(Vec2Fixed delta)
        {
            Vec2Fixed thisDelta = Delta;
            return !thisDelta.X.DifferentSign(delta.X) && !thisDelta.Y.DifferentSign(delta.Y);
        }
        public bool SameDirection(Vector2Fixed delta)
        {
            Vec2Fixed thisDelta = Delta;
            return !thisDelta.X.DifferentSign(delta.X) && !thisDelta.Y.DifferentSign(delta.Y);
        }
        public double PerpDot(Vec2Fixed point)
        {
            return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));
        }
        public double PerpDot(Vector2Fixed point)
        {
            return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));
        }
        public double PerpDot(Vec3Fixed point) => PerpDot(point.XY);
        public double PerpDot(Vector3Fixed point) => PerpDot(point.XY);
        public bool OnRight(Vec2Fixed point) => PerpDot(point) <= 0;
        public bool OnRight(Vector2Fixed point) => PerpDot(point) <= 0;
        public bool OnRight(Vec3Fixed point) => PerpDot(point.XY) <= 0;
        public bool OnRight(Vector3Fixed point) => PerpDot(point.XY) <= 0;
        public bool DifferentSides(Vec2Fixed first, Vec2Fixed second) => OnRight(first) != OnRight(second);
        public bool DifferentSides(Vector2Fixed first, Vector2Fixed second) => OnRight(first) != OnRight(second);
        public bool DifferentSides(Seg2Fixed seg) => OnRight(seg.Start) != OnRight(seg.End);
        public bool DifferentSides(Segment2Fixed seg) => OnRight(seg.Start) != OnRight(seg.End);
        public override string ToString() => $"({Start}), ({End})";
        public override bool Equals(object? obj) => obj is Segment2Fixed seg && Start == seg.Start && End == seg.End;
        public override int GetHashCode() => HashCode.Combine(Start.GetHashCode(), End.GetHashCode());

        private IEnumerable<Vec2Fixed> GetVertices()
        {
            yield return Start;
            yield return End;
        }
        private static bool CollinearHelper(Vec2Fixed first, Vec2Fixed second, Vec2Fixed third)
        {
            return ((first.X * (second.Y - third.Y)) + (second.X * (third.Y - first.Y)) + (third.X * (first.Y - second.Y))).ApproxZero();
        }
        private static double DoubleTriArea(Vec2Fixed first, Vec2Fixed second, Vec2Fixed third)
        {
            return ((first.X - third.X) * (second.Y - third.Y)) - ((first.Y - third.Y) * (second.X - third.X));
        }
    }
}
