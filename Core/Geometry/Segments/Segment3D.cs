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
    public class Segment3D
    {
        public Vec3D Start;
        public Vec3D End;

        public Vec3D Delta => End - Start;
        public double Length => Start.Distance(End);
        public Seg3D Struct => new(Start, End);
        public Box3D Box => new((Start.X.Min(End.X), Start.Y.Min(End.Y), Start.Z.Min(End.Z)), (Start.X.Max(End.X), Start.Y.Max(End.Y), Start.Z.Max(End.Z)));
        public IEnumerable<Vec3D> Vertices => GetVertices();

        public Segment3D(Vec3D start, Vec3D end)
        {
            Start = start;
            End = end;
        }

        public void Deconstruct(out Vec3D start, out Vec3D end)
        {
            start = Start;
            end = End;
        }

        public Vec3D this[int index] => index == 0 ? Start : End;
        public Vec3D this[Endpoint endpoint] => endpoint == Endpoint.Start ? Start : End;

        public static Seg3D operator +(Segment3D self, Vec3D other) => new(self.Start + other, self.End + other);
        public static Seg3D operator +(Segment3D self, Vector3D other) => new(self.Start + other, self.End + other);
        public static Seg3D operator -(Segment3D self, Vec3D other) => new(self.Start - other, self.End - other);
        public static Seg3D operator -(Segment3D self, Vector3D other) => new(self.Start - other, self.End - other);
        public static bool operator ==(Segment3D self, Segment3D other) => self.Start == other.Start && self.End == other.End;
        public static bool operator !=(Segment3D self, Segment3D other) => !(self == other);

        public Vec3D Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;
        public Vec3D FromTime(double t) => Start + (Delta * t);
        public override string ToString() => $"({Start}), ({End})";
        public override bool Equals(object? obj) => obj is Segment3D seg && Start == seg.Start && End == seg.End;
        public override int GetHashCode() => HashCode.Combine(Start.GetHashCode(), End.GetHashCode());

        private IEnumerable<Vec3D> GetVertices()
        {
            yield return Start;
            yield return End;
        }
    }
}
