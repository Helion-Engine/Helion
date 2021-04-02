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
    public class Segment3F
    {
        public Vec3F Start;
        public Vec3F End;

        public Vec3F Delta => End - Start;
        public float Length => Start.Distance(End);
        public Box3F Box => new((Start.X.Min(End.X), Start.Y.Min(End.Y), Start.Z.Min(End.Z)), (Start.X.Max(End.X), Start.Y.Max(End.Y), Start.Z.Max(End.Z)));
        public IEnumerable<Vec3F> Vertices => GetVertices();

        public Segment3F(Vec3F start, Vec3F end)
        {
            Start = start;
            End = end;
        }

        public static implicit operator Segment3F(ValueTuple<Vec3F, Vec3F> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public void Deconstruct(out Vec3F start, out Vec3F end)
        {
            start = Start;
            end = End;
        }

        public Vec3F this[int index] => index == 0 ? Start : End;
        public Vec3F this[Endpoint endpoint] => endpoint == Endpoint.Start ? Start : End;

        public static Seg3F operator +(Segment3F self, Vec3F other) => new(self.Start + other, self.End + other);
        public static Seg3F operator +(Segment3F self, Vector3F other) => new(self.Start + other, self.End + other);
        public static Seg3F operator -(Segment3F self, Vec3F other) => new(self.Start - other, self.End - other);
        public static Seg3F operator -(Segment3F self, Vector3F other) => new(self.Start - other, self.End - other);
        public static bool operator ==(Segment3F self, Segment3F other) => self.Start == other.Start && self.End == other.End;
        public static bool operator !=(Segment3F self, Segment3F other) => !(self == other);

        public Vec3F Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;
        public Seg3F WithStart(Vec3F start) => (start, End);
        public Seg3F WithStart(Vector3F start) => (start.Struct, End);
        public Seg3F WithEnd(Vec3F end) => (Start, end);
        public Seg3F WithEnd(Vector3F end) => (Start, end.Struct);
        public Vec3F FromTime(float t) => Start + (Delta * t);
    }
}
