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
    public class Segment3D<T> where T : Vector3D
    {
        public T Start;
        public T End;

        public Vec3D Delta => End - Start;
        public double Length => Start.Distance(End);
        public Box3D Box => new((Start.X.Min(End.X), Start.Y.Min(End.Y), Start.Z.Min(End.Z)), (Start.X.Max(End.X), Start.Y.Max(End.Y), Start.Z.Max(End.Z)));
        public IEnumerable<T> Vertices => GetVertices();

        public Segment3D(T start, T end)
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

        public static Seg3D operator +(Segment3D<T> self, Vec3D other) => new(self.Start + other, self.End + other);
        public static Seg3D operator +(Segment3D<T> self, T other) => new(self.Start + other, self.End + other);
        public static Seg3D operator -(Segment3D<T> self, Vec3D other) => new(self.Start - other, self.End - other);
        public static Seg3D operator -(Segment3D<T> self, T other) => new(self.Start - other, self.End - other);
        public static bool operator ==(Segment3D<T> self, Segment3D<T> other) => self.Start == other.Start && self.End == other.End;
        public static bool operator !=(Segment3D<T> self, Segment3D<T> other) => !(self == other);

        public T Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;
        public Vec3D FromTime(double t) => Start + (Delta * t);
        public override string ToString() => $"({Start}), ({End})";
        public override bool Equals(object? obj) => obj is Segment3D<T> seg && Start == seg.Start && End == seg.End;
        public override int GetHashCode() => HashCode.Combine(Start.GetHashCode(), End.GetHashCode());

        private IEnumerable<T> GetVertices()
        {
            yield return Start;
            yield return End;
        }
    }
}
