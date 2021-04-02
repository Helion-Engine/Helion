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
    public class SegmentT3F
    {
        public T Start;
        public T End;

        public Vec3F Delta => End - Start;
        public float Length => Start.Distance(End);
        public Box3F Box => new((Start.X.Min(End.X), Start.Y.Min(End.Y), Start.Z.Min(End.Z)), (Start.X.Max(End.X), Start.Y.Max(End.Y), Start.Z.Max(End.Z)));
        public IEnumerable<T> Vertices => GetVertices();

        public SegmentT3F(T start, T end)
        {
            Start = start;
            End = end;
        }

        public static implicit operator SegmentT3F(ValueTuple<T, T> tuple)
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

        public static Seg3F operator +(SegmentT3F self, Vec3F other) => new(self.Start + other, self.End + other);
        public static Seg3F operator +(SegmentT3F self, T other) => new(self.Start + other, self.End + other);
        public static Seg3F operator -(SegmentT3F self, Vec3F other) => new(self.Start - other, self.End - other);
        public static Seg3F operator -(SegmentT3F self, T other) => new(self.Start - other, self.End - other);
        public static bool operator ==(SegmentT3F self, SegmentT3F other) => self.Start == other.Start && self.End == other.End;
        public static bool operator !=(SegmentT3F self, SegmentT3F other) => !(self == other);

        public Vec3F Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;
        public Seg3F WithStart(Vec3F start) => (start, End);
        public Seg3F WithStart(Vector3F start) => (start.Struct, End);
        public Seg3F WithEnd(Vec3F end) => (Start, end);
        public Seg3F WithEnd(Vector3F end) => (Start, end.Struct);
        public Vec3F FromTime(float t) => Start + (Delta * t);
    }
}
