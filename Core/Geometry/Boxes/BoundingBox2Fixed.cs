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

public class BoundingBox2Fixed
{
    protected Vec2Fixed m_Min;
    protected Vec2Fixed m_Max;

    public Vec2Fixed Min => m_Min;
    public Vec2Fixed Max => m_Max;
    public Vec2Fixed TopLeft => new(Min.X, Max.Y);
    public Vec2Fixed BottomLeft => Min;
    public Vec2Fixed BottomRight => new(Max.X, Min.Y);
    public Vec2Fixed TopRight => Max;
    public Fixed Top => Max.Y;
    public Fixed Bottom => Min.Y;
    public Fixed Left => Min.X;
    public Fixed Right => Max.X;
    public Fixed Width => Max.X - Min.X;
    public Fixed Height => Max.Y - Min.Y;
    public Box2I Int => new(Min.Int, Max.Int);
    public Box2F Float => new(Min.Float, Max.Float);
    public Box2D Double => new(Min.Double, Max.Double);
    public Box2Fixed Struct => new(Min, Max);
    public Vec2Fixed Sides => Max - Min;

    public BoundingBox2Fixed(Vec2Fixed min, Vec2Fixed max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        m_Min = min;
        m_Max = max;
    }

    public BoundingBox2Fixed(Vec2Fixed min, Vector2Fixed max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        m_Min = min;
        m_Max = max.Struct;
    }

    public BoundingBox2Fixed(Vector2Fixed min, Vec2Fixed max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        m_Min = min.Struct;
        m_Max = max;
    }

    public BoundingBox2Fixed(Vector2Fixed min, Vector2Fixed max)
    {
        Precondition(min.X <= max.X, "Bounding box min X > max X");
        Precondition(min.Y <= max.Y, "Bounding box min Y > max Y");

        m_Min = min.Struct;
        m_Max = max.Struct;
    }

    public BoundingBox2Fixed(Vec2Fixed center, Fixed radius)
    {
        Precondition(radius >= 0, "Bounding box radius yields min X > max X");

        m_Min = new(center.X - radius, center.Y - radius);
        m_Max = new(center.X + radius, center.Y + radius);
    }

    public BoundingBox2Fixed(Vector2Fixed center, Fixed radius)
    {
        Precondition(radius >= 0, "Bounding box radius yields min X > max X");

        m_Min = new(center.X - radius, center.Y - radius);
        m_Max = new(center.X + radius, center.Y + radius);
    }

    public void Deconstruct(out Vec2Fixed min, out Vec2Fixed max)
    {
        min = Min;
        max = Max;
    }

    public static Box2Fixed operator *(BoundingBox2Fixed self, Fixed scale) => new(self.Min * scale, self.Max * scale);
    public static Box2Fixed operator /(BoundingBox2Fixed self, Fixed divisor) => new(self.Min / divisor, self.Max / divisor);
    public static Box2Fixed operator +(BoundingBox2Fixed self, Vec2Fixed offset) => new(self.Min + offset, self.Max + offset);
    public static Box2Fixed operator +(BoundingBox2Fixed self, Vector2Fixed offset) => new(self.Min + offset, self.Max + offset);
    public static Box2Fixed operator -(BoundingBox2Fixed self, Vec2Fixed offset) => new(self.Min - offset, self.Max - offset);
    public static Box2Fixed operator -(BoundingBox2Fixed self, Vector2Fixed offset) => new(self.Min - offset, self.Max - offset);

    public bool Contains(Vec2Fixed point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
    public bool Contains(Vector2Fixed point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
    public bool Contains(Vec3Fixed point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
    public bool Contains(Vector3Fixed point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
    public bool Overlaps(in Box2Fixed box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
    public bool Overlaps(BoundingBox2Fixed box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
    public Box2Fixed Combine(params Box2Fixed[] boxes)
    {
        Vec2Fixed min = Min;
        Vec2Fixed max = Max;
        for (int i = 0; i < boxes.Length; i++)
        {
            Box2Fixed box = boxes[i];
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
        }
        return new(min, max);
    }
    public Box2Fixed Combine(params BoundingBox2Fixed[] boxes)
    {
        Vec2Fixed min = Min;
        Vec2Fixed max = Max;
        for (int i = 0; i < boxes.Length; i++)
        {
            BoundingBox2Fixed box = boxes[i];
            min.X = min.X.Min(box.Min.X);
            min.Y = min.Y.Min(box.Min.Y);
            max.X = max.X.Max(box.Max.X);
            max.Y = max.Y.Max(box.Max.Y);
        }
        return new(min, max);
    }
    public override string ToString() => $"({Min}), ({Max})";
}

