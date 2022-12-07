using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Geometry.New;

public readonly record struct Box2d(Vec2d Origin, Vec2d Size)
{
    public double X => Origin.X;
    public double Y => Origin.Y;
    public double Width => Size.Width;
    public double Height => Size.Height;
    public Vec2d Min => Origin;
    public Vec2d Max => Origin + Size;
    public AABB2d AABB
    {
        get
        {
            Vec2d half = Size / 2;
            return new(Origin + half, half);
        }
    }

    public Box2d(AABB2d aabb) : this(aabb.Min, aabb.Max)
    {
    }

    public Box2d(Seg2d seg) : this(seg.Start.Min(seg.End), seg.Start.Max(seg.End))
    {
    }

    public static Box2d operator +(Box2d self, Vec2d other) => new(self.Origin + other, self.Size);
    public static Box2d operator -(Box2d self, Vec2d other) => new(self.Origin - other, self.Size);

    public Box2d Bound(Box2d other) => new(Min.Min(other.Min), Max.Max(other.Max));
    public bool Contains(Vec2d point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
    public bool Contains(Vec3d point) => Contains(point.XY);
    public bool Intersects(Box2d box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);
    public bool Intersects(AABB2d aabb) => Intersects(aabb.Box);

    public static Box2d Bound(params Vec2d[] points)
    {
        if (points.Length == 0)
            return default;

        Box2d result = new(points[0], points[0]);
        for (int i = 1; i < points.Length; i++)
            result = new(result.Min.Min(points[i]), result.Max.Max(points[i]));
        return result;
    }
}

public readonly record struct Box3d(Vec3d Origin, Vec3d Size)
{
    public Vec3d Min => Origin;
    public Vec3d Max => Origin + Size;
    public AABB3d AABB
    {
        get
        {
            Vec3d half = Size / 2;
            return new(Origin + half, half);
        }
    }

    public Box3d(AABB3d aabb) : this(aabb.Min, aabb.Max)
    {
    }

    public Box3d(Seg3d seg) : this(seg.Start.Min(seg.End), seg.Start.Max(seg.End))
    {
    }

    public static Box3d operator +(Box3d self, Vec3d other) => new(self.Origin + other, self.Size);
    public static Box3d operator -(Box3d self, Vec3d other) => new(self.Origin - other, self.Size);

    public Box3d Bound(Box3d other) => new(Min.Min(other.Min), Max.Max(other.Max));
    public bool Contains(Vec3d point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y && point.Z > Min.Z && point.Z < Max.Z;
    public bool Intersects(Box3d box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z);
    public bool Intersects(AABB3d aabb) => Intersects(aabb.Box);
}
