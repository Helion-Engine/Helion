using System;

namespace Helion.GeometryNew;

public readonly record struct Seg2d(Vec2d Start, Vec2d End) :
    IAdditionOperators<Seg2d, Vec2d, Seg2d>,
    ISubtractionOperators<Seg2d, Vec2d, Seg2d>
{
    public Vec2d Delta => End - Start;
    public double Length => Start.Distance(End);
    public Box2d Box => new(this);

    public static implicit operator Seg2d(ValueTuple<Vec2d, Vec2d> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public void Deconstruct(out Vec2d start, out Vec2d end)
    {
        start = Start;
        end = End;
    }

    public static Seg2d operator +(Seg2d self, Vec2d other) => new(self.Start + other, self.End + other);
    public static Seg2d operator -(Seg2d self, Vec2d other) => new(self.Start - other, self.End - other);

    public bool DifferentSides(Vec2d first, Vec2d second) => OnRight(first) != OnRight(second);
    public bool OnRight(Vec2d point) => PerpDot(point) <= 0;
    public bool OnRight(Vec3d point) => OnRight(point.XY);
    public Vec2d FromTime(double t) => Start + (Delta * t);

    public double PerpDot(Vec2d point)
    {
        Vec2d delta = Delta;
        return (delta.X * (point.Y - Start.Y)) - (delta.Y * (point.X - Start.X));
    }

    public Vec2d ClosestPoint(Vec2d point)
    {
        Vec2d pointToStartDelta = Start - point;
        double t = -Delta.Dot(pointToStartDelta) / Delta.Dot(Delta);

        if (t <= 0)
            return Start;
        if (t >= 1)
            return End;
        return FromTime(t);
    }

    public override string ToString() => $"({Start}), ({End})";
}

public readonly record struct Seg3d(Vec3d Start, Vec3d End) :
    IAdditionOperators<Seg3d, Vec3d, Seg3d>,
    ISubtractionOperators<Seg3d, Vec3d, Seg3d>
{
    public Vec3d Delta => End - Start;
    public double Length => Start.Distance(End);

    public static implicit operator Seg3d(ValueTuple<Vec3d, Vec3d> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public void Deconstruct(out Vec3d start, out Vec3d end)
    {
        start = Start;
        end = End;
    }

    public static Seg3d operator +(Seg3d self, Vec3d other) => new(self.Start + other, self.End + other);
    public static Seg3d operator -(Seg3d self, Vec3d other) => new(self.Start - other, self.End - other);

    public Vec3d FromTime(double t) => Start + (Delta * t);

    public override string ToString() => $"({Start}), ({End})";
}
