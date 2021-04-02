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
    public class SegmentT3F<T> where T : Vector3F
    {
        public T Start;
        public T End;

        public Vec3F Delta => End - Start;
        public float Length => Start.Distance(End);
        public Seg3F Struct => new(Start, End);
        public Box3F Box => new((Start.X.Min(End.X), Start.Y.Min(End.Y), Start.Z.Min(End.Z)), (Start.X.Max(End.X), Start.Y.Max(End.Y), Start.Z.Max(End.Z)));
        public IEnumerable<T> Vertices => GetVertices();

        public SegmentT3F(T start, T end)
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

        public static Seg3F operator +(SegmentT3F<T> self, Vec3F other) => new(self.Start + other, self.End + other);
        public static Seg3F operator +(SegmentT3F<T> self, T other) => new(self.Start + other, self.End + other);
        public static Seg3F operator -(SegmentT3F<T> self, Vec3F other) => new(self.Start - other, self.End - other);
        public static Seg3F operator -(SegmentT3F<T> self, T other) => new(self.Start - other, self.End - other);

        public T Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;
        public Vec3F FromTime(float t) => Start + (Delta * t);
        public override string ToString() => $"({Start}), ({End})";
        public override bool Equals(object? obj) => obj is SegmentT3F<T> seg && Start == seg.Start && End == seg.End;
        public override int GetHashCode() => HashCode.Combine(Start.GetHashCode(), End.GetHashCode());

        private IEnumerable<T> GetVertices()
        {
            yield return Start;
            yield return End;
        }
    }
}
