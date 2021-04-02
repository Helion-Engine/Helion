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
    public struct Seg3Fixed
    {
        public Vec3Fixed Start;
        public Vec3Fixed End;

        public Vec3Fixed Delta => End - Start;
        public Fixed Length => Start.Distance(End);
        public Box3Fixed Box => new((Start.X.Min(End.X), Start.Y.Min(End.Y), Start.Z.Min(End.Z)), (Start.X.Max(End.X), Start.Y.Max(End.Y), Start.Z.Max(End.Z)));
        public IEnumerable<Vec3Fixed> Vertices => GetVertices();

        public Seg3Fixed(Vec3Fixed start, Vec3Fixed end)
        {
            Start = start;
            End = end;
        }

        public static implicit operator Seg3Fixed(ValueTuple<Vec3Fixed, Vec3Fixed> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public void Deconstruct(out Vec3Fixed start, out Vec3Fixed end)
        {
            start = Start;
            end = End;
        }

        public Vec3Fixed this[int index] => index == 0 ? Start : End;
        public Vec3Fixed this[Endpoint endpoint] => endpoint == Endpoint.Start ? Start : End;

        public static Seg3Fixed operator +(Seg3Fixed self, Vec3Fixed other) => new(self.Start + other, self.End + other);
        public static Seg3Fixed operator +(Seg3Fixed self, Vector3Fixed other) => new(self.Start + other, self.End + other);
        public static Seg3Fixed operator -(Seg3Fixed self, Vec3Fixed other) => new(self.Start - other, self.End - other);
        public static Seg3Fixed operator -(Seg3Fixed self, Vector3Fixed other) => new(self.Start - other, self.End - other);
        public static bool operator ==(Seg3Fixed self, Seg3Fixed other) => self.Start == other.Start && self.End == other.End;
        public static bool operator !=(Seg3Fixed self, Seg3Fixed other) => !(self == other);

        public Vec3Fixed Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;
        public Seg3Fixed WithStart(Vec3Fixed start) => (start, End);
        public Seg3Fixed WithStart(Vector3Fixed start) => (start.Struct, End);
        public Seg3Fixed WithEnd(Vec3Fixed end) => (Start, end);
        public Seg3Fixed WithEnd(Vector3Fixed end) => (Start, end.Struct);
        public Vec3Fixed FromTime(Fixed t) => Start + (Delta * t);
    }
}
