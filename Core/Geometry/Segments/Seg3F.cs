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
    public struct Seg3F 
    {
        public Vec3F Start;
        public Vec3F End;

        public Vec3F Delta => End - Start;
        public Box3F Box => new((Start.X.Min(End.X), Start.Y.Min(End.Y), Start.Z.Min(End.Z)), (Start.X.Max(End.X), Start.Y.Max(End.Y), Start.Z.Max(End.Z)));
        public float Length => Start.Distance(End);
        public IEnumerable<Vec3F> Vertices => GetVertices();

        public Seg3F(Vec3F start, Vec3F end)
        {
            Start = start;
            End = end;
        }
        public Seg3F(Vec3F start, Vector3F end)
        {
            Start = start;
            End = end.Struct;
        }
        public Seg3F(Vector3F start, Vec3F end)
        {
            Start = start.Struct;
            End = end;
        }
        public Seg3F(Vector3F start, Vector3F end)
        {
            Start = start.Struct;
            End = end.Struct;
        }

        public static implicit operator Seg3F(ValueTuple<Vec3F, Vec3F> tuple)
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

        public static Seg3F operator +(Seg3F self, Vec3F other) => new(self.Start + other, self.End + other);
        public static Seg3F operator +(Seg3F self, Vector3F other) => new(self.Start + other, self.End + other);
        public static Seg3F operator -(Seg3F self, Vec3F other) => new(self.Start - other, self.End - other);
        public static Seg3F operator -(Seg3F self, Vector3F other) => new(self.Start - other, self.End - other);
        public static bool operator ==(Seg3F self, Seg3F other) => self.Start == other.Start && self.End == other.End;
        public static bool operator !=(Seg3F self, Seg3F other) => !(self == other);

        public Vec3F Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;
        public Seg3F WithStart(Vec3F start) => (start, End);
        public Seg3F WithStart(Vector3F start) => (start.Struct, End);
        public Seg3F WithEnd(Vec3F end) => (Start, end);
        public Seg3F WithEnd(Vector3F end) => (Start, end.Struct);
        public Vec3F FromTime(float t) => Start + (Delta * t);
        public override string ToString() => $"({Start}), ({End})";
        public override bool Equals(object? obj) => obj is Seg3F seg && Start == seg.Start && End == seg.End;
        public override int GetHashCode() => HashCode.Combine(Start.GetHashCode(), End.GetHashCode());

        private IEnumerable<Vec3F> GetVertices()
        {
            yield return Start;
            yield return End;
        }
    }
}
