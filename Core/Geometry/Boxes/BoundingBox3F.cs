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
    public class BoundingBox3F
    {
        protected Vec3F m_Min;
        protected Vec3F m_Max;

        public Vec3F Min => m_Min;
        public Vec3F Max => m_Max;
        public Box3F Struct => new(Min, Max);
        public Vec3F Sides => Max - Min;

        public BoundingBox3F(Vec3F min, Vec3F max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            m_Min = min;
            m_Max = max;
        }

        public BoundingBox3F(Vec3F min, Vector3F max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            m_Min = min;
            m_Max = max.Struct;
        }

        public BoundingBox3F(Vector3F min, Vec3F max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            m_Min = min.Struct;
            m_Max = max;
        }

        public BoundingBox3F(Vector3F min, Vector3F max)
        {
            Precondition(min.X <= max.X, "Bounding box min X > max X");
            Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

            m_Min = min.Struct;
            m_Max = max.Struct;
        }

        public void Deconstruct(out Vec3F min, out Vec3F max)
        {
            min = Min;
            max = Max;
        }

        public static Box3F operator +(BoundingBox3F self, Vec3F offset) => new(self.Min + offset, self.Max + offset);
        public static Box3F operator +(BoundingBox3F self, Vector3F offset) => new(self.Min + offset, self.Max + offset);
        public static Box3F operator -(BoundingBox3F self, Vec3F offset) => new(self.Min - offset, self.Max - offset);
        public static Box3F operator -(BoundingBox3F self, Vector3F offset) => new(self.Min - offset, self.Max - offset);

        public bool Contains(Vec3F point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y && point.Z > Min.Z && point.Z < Max.Z;
        public bool Contains(Vector3F point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y && point.Z > Min.Z && point.Z < Max.Z;
        public bool Overlaps2D(in Box2F other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
        public bool Overlaps2D(in Box3F other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
        public bool Overlaps2D(BoundingBox2F other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
        public bool Overlaps2D(BoundingBox3F other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
        public bool Overlaps(in Box3F box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
        public bool Overlaps(BoundingBox3F box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
        public Box3F Combine(params Box3F[] boxes)
        {
            Vec3F min = Min;
            Vec3F max = Max;
            for (int i = 0; i < boxes.Length; i++)
            {
                Box3F box = boxes[i];
                min.X = min.X.Min(box.Min.X);
                min.Y = min.Y.Min(box.Min.Y);
                min.Z = min.Z.Min(box.Min.Z);
                max.X = max.X.Max(box.Max.X);
                max.Y = max.Y.Max(box.Max.Y);
                max.Z = max.Z.Max(box.Max.Z);
            }
            return new(min, max);
        }
        public Box3F Combine(params BoundingBox3F[] boxes)
        {
            Vec3F min = Min;
            Vec3F max = Max;
            for (int i = 0; i < boxes.Length; i++)
            {
                BoundingBox3F box = boxes[i];
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
