using Helion.Util.Extensions;
using System;

namespace Helion.Geometry.New;

public readonly record struct Seg2d(Vec2d Start, Vec2d End) :
    IAdditionOperators<Seg2d, Vec2d, Seg2d>,
    ISubtractionOperators<Seg2d, Vec2d, Seg2d>
{
    public Vec2d Delta => End - Start;
    public double Length => Start.Distance(End);
    public Box2d Box => new(this);
    public AABB2d AABB => new(this);
    public Ray2d Ray => new(Start, Delta.Unit);

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
    public static Seg2d operator *(Seg2d self, double scale)
    {
        Vec2d scaledDelta = self.Delta * scale;
        return (self.Start, self.Start + scaledDelta);
    }

    public Vec2d FromTime(double t) => Start + (Delta * t);
    public bool OnRight(Vec2d point) => PerpDot(point) <= 0;
    public bool OnRight(Vec3d point) => OnRight(point.XY);
    public bool DifferentSides(Vec2d first, Vec2d second) => OnRight(first) != OnRight(second);
    public double DoubleTriArea(Vec2d point) => new Triangle2d(Start, End, point).DoubleTriArea();

    public double PerpDot(Vec2d point)
    {
        Vec2d delta = Delta;
        return (delta.X * (point.Y - Start.Y)) - (delta.Y * (point.X - Start.X));
    }

    public bool Parallel(Seg2d seg, double epsilon = double.Epsilon)
    {
        Vec2d thisDelta = Delta;
        Vec2d otherDelta = seg.Delta;
        return (thisDelta.Y * otherDelta.X).ApproxEquals(thisDelta.X * otherDelta.Y, epsilon);
    }

    public bool Collinear(Seg2d seg)
    {
        return CollinearHelper(seg.Start, Start, End) && CollinearHelper(seg.End, Start, End);
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

    public bool TryIntersect(Seg2d seg, out double t)
    {
        // TODO: Is this right? The ordering of the last args?
        double areaStart = DoubleTriArea(seg.End);
        double areaEnd = DoubleTriArea(seg.Start);
        if (areaStart.DifferentSign(areaEnd))
        {
            double areaThisStart = seg.DoubleTriArea(Start);
            double areaThisEnd = seg.DoubleTriArea(End);
            if (areaThisStart.DifferentSign(areaThisEnd))
            {
                t = areaThisStart / (areaThisStart - areaThisEnd);
                return t >= 0 && t <= 1;
            }
        }

        t = default;
        return false;
    }

    public bool TryIntersect(Seg2d seg, out Vec2d point)
    {
        if (TryIntersect(seg, out double t))
        {
            point = FromTime(t);
            return true;
        }

        point = default;
        return false;
    }

    // Treats the current segment as a line and finds the intersection `t` along
    // this line with the target segment.
    public bool TryLineIntersect(Seg2d seg, out double t)
    {
        Vec2d thisDelta = Delta;
        Vec2d otherDelta = seg.Delta;
        double determinant = (-otherDelta.X * thisDelta.Y) + (thisDelta.X * otherDelta.Y);
        if (determinant.ApproxZero())
        {
            t = default;
            return false;
        }

        Vec2d startDelta = Start - seg.Start;
        t = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) / determinant;
        return true;
    }

    public bool TryLineIntersect(Seg2d seg, out Vec2d point)
    {
        if (TryLineIntersect(seg, out double t))
        {
            point = FromTime(t);
            return true;
        }

        point = default;
        return false;
    }

    private static bool CollinearHelper(Vec2d a, Vec2d b, Vec2d c)
    {
        return ((a.X * (b.Y - c.Y)) + (b.X * (c.Y - a.Y)) + (c.X * (a.Y - b.Y))).ApproxZero();
    }

    public override string ToString() => $"({Start}), ({End})";
}

public readonly record struct Seg3d(Vec3d Start, Vec3d End) :
    IAdditionOperators<Seg3d, Vec3d, Seg3d>,
    ISubtractionOperators<Seg3d, Vec3d, Seg3d>
{
    public Vec3d Delta => End - Start;
    public double Length => Start.Distance(End);
    public Box3d Box => new(this);
    public AABB3d AABB => new(this);
    public Ray3d Ray => new(Start, Delta.Unit);

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
