// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Geometry.Boxes;

public class BoundingBox3D
{
    protected Vec3D m_Min;
    protected Vec3D m_Max;

    public Vec3D Min => m_Min;
    public Vec3D Max => m_Max;
    public Box3I Int => new(Min.Int, Max.Int);
    public Box3F Float => new(Min.Float, Max.Float);
    public Box3D Struct => new(Min, Max);
    public Vec3D Sides => Max - Min;

    public BoundingBox3D(Vec3D min, Vec3D max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        m_Min = min;
        m_Max = max;
    }

    public BoundingBox3D(Vec3D min, Vector3D max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        m_Min = min;
        m_Max = max.Struct;
    }

    public BoundingBox3D(Vector3D min, Vec3D max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        m_Min = min.Struct;
        m_Max = max;
    }

    public BoundingBox3D(Vector3D min, Vector3D max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        m_Min = min.Struct;
        m_Max = max.Struct;
    }

    public void Deconstruct(out Vec3D min, out Vec3D max)
    {
        min = Min;
        max = Max;
    }

    public static Box3D operator *(BoundingBox3D self, double scale) => new(self.Min * scale, self.Max * scale);
    public static Box3D operator /(BoundingBox3D self, double divisor) => new(self.Min / divisor, self.Max / divisor);
    public static Box3D operator +(BoundingBox3D self, Vec3D offset) => new(self.Min + offset, self.Max + offset);
    public static Box3D operator +(BoundingBox3D self, Vector3D offset) => new(self.Min + offset, self.Max + offset);
    public static Box3D operator -(BoundingBox3D self, Vec3D offset) => new(self.Min - offset, self.Max - offset);
    public static Box3D operator -(BoundingBox3D self, Vector3D offset) => new(self.Min - offset, self.Max - offset);

    public bool Contains(Vec3D point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y && point.Z > Min.Z && point.Z < Max.Z;
    public bool Contains(Vector3D point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y && point.Z > Min.Z && point.Z < Max.Z;
    public bool Overlaps2D(in Box2D other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
    public bool Overlaps2D(in Box3D other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
    public bool Overlaps2D(BoundingBox2D other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
    public bool Overlaps2D(BoundingBox3D other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);
    public bool Overlaps(in Box3D box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
    public bool Overlaps(BoundingBox3D box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
    public Box3D Combine(params Box3D[] boxes)
    {
        Vec3D min = Min;
        Vec3D max = Max;
        for (int i = 0; i < boxes.Length; i++)
        {
            Box3D box = boxes[i];
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            min.Z = min.Z.Min(box.Min.Z);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
            max.Z = max.Z.Max(box.Max.Z);
        }
        return new(min, max);
    }
    public Box3D Combine(params BoundingBox3D[] boxes)
    {
        Vec3D min = Min;
        Vec3D max = Max;
        for (int i = 0; i < boxes.Length; i++)
        {
            BoundingBox3D box = boxes[i];
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
