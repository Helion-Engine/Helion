// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Geometry.Boxes
{
    public readonly struct Box3Fixed
    {
        public readonly Vec3Fixed Min;
        public readonly Vec3Fixed Max;

        public Vec3Fixed Sides => Max - Min;

        public Box3Fixed(Vec3Fixed min, Vec3Fixed max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min;
            Max = max;
        }

        public Box3Fixed(Vec3Fixed min, Vector3Fixed max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min;
            Max = max.Struct;
        }

        public Box3Fixed(Vector3Fixed min, Vec3Fixed max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min.Struct;
            Max = max;
        }

        public Box3Fixed(Vector3Fixed min, Vector3Fixed max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            Min = min.Struct;
            Max = max.Struct;
        }

        public static implicit operator Box3Fixed(ValueTuple<Vec3Fixed, Vec3Fixed> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Box3Fixed(ValueTuple<Vec3Fixed, Vector3Fixed> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Box3Fixed(ValueTuple<Vector3Fixed, Vec3Fixed> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static implicit operator Box3Fixed(ValueTuple<Vector3Fixed, Vector3Fixed> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public void Deconstruct(out Vec3Fixed min, out Vec3Fixed max)
        {
            min = Min;
            max = Max;
        }

        public static Box3Fixed operator +(Box3Fixed self, Vec3Fixed offset) => new(self.Min + offset, self.Max + offset);
        public static Box3Fixed operator +(Box3Fixed self, Vector3Fixed offset) => new(self.Min + offset, self.Max + offset);
        public static Box3Fixed operator -(Box3Fixed self, Vec3Fixed offset) => new(self.Min - offset, self.Max - offset);
        public static Box3Fixed operator -(Box3Fixed self, Vector3Fixed offset) => new(self.Min - offset, self.Max - offset);

        public bool Contains(Vec3Fixed point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y && point.Z > Min.Z && point.Z < Max.Z;
        public bool Contains(Vector3Fixed point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y && point.Z > Min.Z && point.Z < Max.Z;
        public bool Overlaps2D(in Box2Fixed other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
        public bool Overlaps2D(in Box3Fixed other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
        public bool Overlaps2D(BoundingBox2Fixed other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
        public bool Overlaps2D(BoundingBox3Fixed other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
        public bool Overlaps(in Box3Fixed box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
        public bool Overlaps(BoundingBox3Fixed box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
        public Box3Fixed Combine(params Box3Fixed[] boxes)
        {
            Vec3Fixed min = Min;
            Vec3Fixed max = Max;
            for (int i = 0; i < boxes.Length; i++)
            {
                Box3Fixed box = boxes[i];
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                min.Z = min.Z.Min(box.Min.Z);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
                max.Z = max.Z.Max(box.Max.Z);
            }
            return new(min, max);
        }
        public Box3Fixed Combine(params BoundingBox3Fixed[] boxes)
        {
            Vec3Fixed min = Min;
            Vec3Fixed max = Max;
            for (int i = 0; i < boxes.Length; i++)
            {
                BoundingBox3Fixed box = boxes[i];
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
