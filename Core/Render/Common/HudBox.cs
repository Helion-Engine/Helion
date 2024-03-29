using System;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;

namespace Helion.Render.Common;

public readonly struct HudBox
{
    public readonly Vec2I Min;
    public readonly Vec2I Max;

    public Vec2I TopLeft => Min;
    public Vec2I BottomLeft => (Min.X, Max.Y);
    public Vec2I BottomRight => Max;
    public Vec2I TopRight => (Max.X, Min.Y);
    public int Top => Min.Y;
    public int Bottom => Max.Y;
    public int Left => Min.X;
    public int Right => Max.X;
    public int Width => Max.X - Min.X;
    public int Height => Max.Y - Min.Y;
    public Dimension Dimension => new(Width, Height);
    public Vec2I Sides => new(Width, Height);

    public HudBox(Box2I box)
    {
        Min = box.Min;
        Max = box.Max;
    }

    public HudBox(Vec2I min, Vec2I max)
    {
        Min = min;
        Max = max;
    }

    public HudBox(Vec2I min, Vector2I max)
    {
        Min = min;
        Max = max.Struct;
    }

    public HudBox(Vector2I min, Vec2I max)
    {
        Min = min.Struct;
        Max = max;
    }

    public HudBox(Vector2I min, Vector2I max)
    {
        Min = min.Struct;
        Max = max.Struct;
    }

    public static implicit operator HudBox(ValueTuple<int, int, int, int> tuple)
    {
        return new((tuple.Item1, tuple.Item2), (tuple.Item3, tuple.Item4));
    }

    public static implicit operator HudBox(ValueTuple<Vec2I, Vec2I> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public static implicit operator HudBox(ValueTuple<Vec2I, Vector2I> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public static implicit operator HudBox(ValueTuple<Vector2I, Vec2I> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public static implicit operator HudBox(ValueTuple<Vector2I, Vector2I> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public static HudBox operator +(HudBox box, Vec2I point) => (box.Min + point, box.Max + point);
    public static HudBox operator -(HudBox box, Vec2I point) => (box.Min - point, box.Max - point);
    public static HudBox operator *(HudBox box, int scale) => (box.Min * scale, box.Max * scale);
    public static HudBox operator /(HudBox box, int divisor) => (box.Min / divisor, box.Max / divisor);

    public void Deconstruct(out Vec2I min, out Vec2I max)
    {
        min = Min;
        max = Max;
    }

    public override string ToString() => $"({Min}), ({Max})";
}
