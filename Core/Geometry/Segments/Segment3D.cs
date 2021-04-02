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
        public Box3D Box => new((Start.X.Min(End.X), Start.Y.Min(End.Y), Start.Z.Min(End.Z)), (Start.X.Max(End.X), Start.Y.Max(End.Y), Start.Z.Max(End.Z)));
        public IEnumerable<Vec3D> Vertices => GetVertices();

        public Segment3D(Vec3D start, Vec3D end)
        {
            Start = start;
            End = end;
        }

        public static implicit operator Segment3D(ValueTuple<Vec3D, Vec3D> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
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
        public Seg3D WithStart(Vec3D start) => (start, End);
        public Seg3D WithStart(Vector3D start) => (start.Struct, End);
        public Seg3D WithEnd(Vec3D end) => (Start, end);
        public Seg3D WithEnd(Vector3D end) => (Start, end.Struct);
        public Vec3D FromTime(double t) => Start + (Delta * t);
    }
}
