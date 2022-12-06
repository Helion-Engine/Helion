using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Helion.GeometryNew;

public readonly record struct Box2d(Vec2d Min, Vec2d Max)
{
    public double Top => Max.Y;
    public double Bottom => Min.Y;
    public double Left => Min.X;
    public double Right => Max.X;
    public double Width => Max.X - Min.X;
    public double Height => Max.Y - Min.Y;
    public double Area => Width * Height;
    public Vec2d TopLeft => new(Min.X, Max.Y);
    public Vec2d BottomLeft => Min;
    public Vec2d BottomRight => new(Max.X, Min.Y);
    public Vec2d TopRight => Max;
    public Vec2d Sides => Max - Min;

    public Box2d(Vec2d center, double radius) : 
        this((center.X - radius, center.Y - radius), (center.X + radius, center.Y + radius)) 
    {
    }

    public Box2d(Seg2d seg) : 
        this(seg.Start.Min(seg.End), seg.Start.Max(seg.End))
    {
    }

    public static implicit operator Box2d(ValueTuple<double, double, double, double> tuple)
    {
        return new((tuple.Item1, tuple.Item2), (tuple.Item3, tuple.Item4));
    }

    public static implicit operator Box2d(ValueTuple<Vec2d, Vec2d> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public static Box2d operator +(Box2d self, Vec2d offset) => new(self.Min + offset, self.Max + offset);
    public static Box2d operator -(Box2d self, Vec2d offset) => new(self.Min - offset, self.Max - offset);

    public static Box2d Bound(params Vec2d[] points)
    {
        if (points.Length == 0)
            return default;

        Box2d result = (points[0], points[0]);
        for (int i = 1; i < points.Length; i++)
            result = (result.Min.Min(points[i]), result.Max.Max(points[i]));
        return result;
    }

    public static Box2d Bound(params Box2d[] boxes)
    {
        if (boxes.Length == 0)
            return default;

        Box2d result = boxes[0];
        for (int i = 1; i < boxes.Length; i++)
            result = result.Bound(boxes[i]);
        return result;
    }

    public static Box2d Bound(params Seg2d[] segs)
    {
        if (segs.Length == 0)
            return default;

        Box2d result = new(segs[0]);
        for (int i = 1; i < segs.Length; i++)
            result = result.Bound(new(segs[i]));
        return result;
    }

    public Box2d Bound(Box2d other) => (Min.Min(other.Min), Max.Max(other.Max));
    public bool Contains(Vec2d point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
    public bool Contains(Vec3d point) => point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y;
    public bool Overlaps(Box2d box) => !(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y);

    public bool Intersects(Seg2d seg)
    {
        return false;
    }

    public bool TryClip(ConvexPolygon2D polygon, [NotNullWhen(true)] out ConvexPolygon2D? result)
    {
        // TODO
        result = null;
        return false;
    }

    public override string ToString() => $"({Min}), ({Max})";
}