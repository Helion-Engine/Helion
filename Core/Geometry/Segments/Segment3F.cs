﻿// THIS FILE WAS AUTO-GENERATED.
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
        public Seg3F Struct => new(Start, End);
        public Box3F Box => new((Start.X.Min(End.X), Start.Y.Min(End.Y), Start.Z.Min(End.Z)), (Start.X.Max(End.X), Start.Y.Max(End.Y), Start.Z.Max(End.Z)));
        public IEnumerable<Vec3F> Vertices => GetVertices();

        public Segment3F(Vec3F start, Vec3F end)
        {
            Start = start;
            End = end;
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
        public Vec3F FromTime(float t) => Start + (Delta * t);
        public override string ToString() => $"({Start}), ({End})";
        public override bool Equals(object? obj) => obj is Segment3F seg && Start == seg.Start && End == seg.End;
        public override int GetHashCode() => HashCode.Combine(Start.GetHashCode(), End.GetHashCode());

        private IEnumerable<Vec3F> GetVertices()
        {
            yield return Start;
            yield return End;
        }
    }
}
