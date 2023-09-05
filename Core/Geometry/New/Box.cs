using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Helion.Geometry.New;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct Box2(Vec2 Min, Vec2 Max)
{
    public Vec2 Sides => Max - Min;
    public Vec2 Center => Min + (Sides * 0.5f);

    public Box2(Seg2 seg) : this(seg.Start.Min(seg.End), seg.Start.Max(seg.End))
    {
    }

    public static Box2 operator +(Box2 self, Vec2 other) => new(self.Min + other, self.Max + other);
    public static Box2 operator -(Box2 self, Vec2 other) => new(self.Min - other, self.Max - other);

    public Box2 Bound(Box2 other) => new(Min.Min(other.Min), Max.Max(other.Max));
    public bool Contains(Vec2 point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
    public bool Contains(Vec3 point) => Contains(point.XY);
    public bool Intersects(Box2 box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);

    public static Box2 Bound(params Vec2[] points)
    {
        if (points.Length == 0)
            return default;

        Box2 result = new(points[0], points[0]);
        for (int i = 1; i < points.Length; i++)
            result = new(result.Min.Min(points[i]), result.Max.Max(points[i]));
        return result;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct Box3(Vec3 Min, Vec3 Max)
{
    public Vec3 Sides => Max - Min;
    public Vec3 Center => Min + (Sides * 0.5f);

    public Box3(Seg3 seg) : this(seg.Start.Min(seg.End), seg.Start.Max(seg.End))
    {
    }

    public static Box3 operator +(Box3 self, Vec3 other) => new(self.Min + other, self.Max + other);
    public static Box3 operator -(Box3 self, Vec3 other) => new(self.Min - other, self.Max - other);

    public Box3 Bound(Box3 other) => new(Min.Min(other.Min), Max.Max(other.Max));
    public bool Contains(Vec3 point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y && point.Z > Min.Z && point.Z < Max.Z;
    public bool Intersects(Box3 box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
}
