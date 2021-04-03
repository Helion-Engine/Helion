// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Geometry.Boxes
{
    public readonly struct Box3I
    {
        public static readonly Box3I UnitBox = ((0, 0, 0), (1, 1, 1));

        public readonly Vec3I Min;
        public readonly Vec3I Max;

        public Vec3I Sides => Max - Min;

        public Box3I(Vec3I min, Vec3I max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");
            Min = min;
            Max = max;
        }

        public Box3I(Vec3I min, Vector3I max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");
            Min = min;
            Max = max.Struct;
        }

        public Box3I(Vector3I min, Vec3I max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");
            Min = min.Struct;
            Max = max;
        }

        public Box3I(Vector3I min, Vector3I max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");
            Min = min.Struct;
            Max = max.Struct;
        }

        public static implicit operator Box3I(ValueTuple<Vec3I, Vec3I> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Box3I(ValueTuple<Vec3I, Vector3I> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Box3I(ValueTuple<Vector3I, Vec3I> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Box3I(ValueTuple<Vector3I, Vector3I> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public void Deconstruct(out Vec3I min, out Vec3I max)
        {
            min = Min;
            max = Max;
        }

        public static Box3I operator +(Box3I self, Vec3I offset) => new(self.Min + offset, self.Max + offset);
        public static Box3I operator +(Box3I self, Vector3I offset) => new(self.Min + offset, self.Max + offset);
        public static Box3I operator -(Box3I self, Vec3I offset) => new(self.Min - offset, self.Max - offset);
        public static Box3I operator -(Box3I self, Vector3I offset) => new(self.Min - offset, self.Max - offset);

        public bool Contains(Vec3I point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y && point.Z > Min.Z && point.Z < Max.Z;
        public bool Contains(Vector3I point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y && point.Z > Min.Z && point.Z < Max.Z;
        public bool Overlaps2D(in Box2I other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
        public bool Overlaps2D(in Box3I other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
        public bool Overlaps2D(BoundingBox2I other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
        public bool Overlaps2D(BoundingBox3I other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
        public bool Overlaps(in Box3I box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
        public bool Overlaps(BoundingBox3I box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
        public Box3I Combine(params Box3I[] boxes)
        {
            Vec3I min = Min;
            Vec3I max = Max;
            for (int i = 0; i < boxes.Length; i++)
            {
                Box3I box = boxes[i];
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                min.Z = min.Z.Min(box.Min.Z);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
                max.Z = max.Z.Max(box.Max.Z);
            }
            return new(min, max);
        }
        public Box3I Combine(params BoundingBox3I[] boxes)
        {
            Vec3I min = Min;
            Vec3I max = Max;
            for (int i = 0; i < boxes.Length; i++)
            {
                BoundingBox3I box = boxes[i];
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                min.Z = min.Z.Min(box.Min.Z);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
                max.Z = max.Z.Max(box.Max.Z);
            }
            return new(min, max);
        }
        public override string ToString() => $"({Min}), ({Max})";
    }
}
