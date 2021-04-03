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
    public class SegmentT3D<V> where V : Vector3D
    {
        public readonly V Start;
        public readonly V End;
        public readonly Vec3D Delta;
        public readonly Box3D Box;

        public double Length => Start.Distance(End);
        public Seg3D Struct => new(Start, End);
        public IEnumerable<V> Vertices => GetVertices();

        public SegmentT3D(V start, V end)
        {
            Start = start;
            End = end;
            Delta = End - Start;
            Box = new((Start.X.Min(End.X), Start.Y.Min(End.Y), Start.Z.Min(End.Z)), (Start.X.Max(End.X), Start.Y.Max(End.Y), Start.Z.Max(End.Z)));
        }

        public void Deconstruct(out V start, out V end)
        {
            start = Start;
            end = End;
        }

        public V this[int index] => index == 0 ? Start : End;
        public V this[Endpoint endpoint] => endpoint == Endpoint.Start ? Start : End;

        public static Seg3D operator +(SegmentT3D<V> self, Vec3D other) => new(self.Start + other, self.End + other);
        public static Seg3D operator +(SegmentT3D<V> self, Vector3D other) => new(self.Start + other, self.End + other);
        public static Seg3D operator -(SegmentT3D<V> self, Vec3D other) => new(self.Start - other, self.End - other);
        public static Seg3D operator -(SegmentT3D<V> self, Vector3D other) => new(self.Start - other, self.End - other);

        public V Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;
        public Vec3D FromTime(double t) => Start + (Delta * t);
        public override string ToString() => $"({Start}), ({End})";
        public override bool Equals(object? obj) => obj is SegmentT3D<V> seg && Start == seg.Start && End == seg.End;
        public override int GetHashCode() => HashCode.Combine(Start.GetHashCode(), End.GetHashCode());

        private IEnumerable<V> GetVertices()
        {
            yield return Start;
            yield return End;
        }
    }
}
