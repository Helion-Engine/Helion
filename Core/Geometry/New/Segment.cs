using Helion.Util.Extensions;
using System;
using System.Runtime.InteropServices;

namespace Helion.Geometry.New;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct Seg2(Vec2 Start, Vec2 End) :
    IAdditionOperators<Seg2, Vec2, Seg2>,
    ISubtractionOperators<Seg2, Vec2, Seg2>
{
    public Vec2 Delta => End - Start;
    public float Length => Start.Distance(End);
    public Box2 Box => new(this);
    public Ray2 Ray => new(Start, Delta.Unit);

    public static implicit operator Seg2(ValueTuple<Vec2, Vec2> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public void Deconstruct(out Vec2 start, out Vec2 end)
    {
        start = Start;
        end = End;
    }

    public static Seg2 operator +(Seg2 self, Vec2 other) => new(self.Start + other, self.End + other);
    public static Seg2 operator -(Seg2 self, Vec2 other) => new(self.Start - other, self.End - other);
    public static Seg2 operator *(Seg2 self, float scale)
    {
        Vec2 scaledDelta = self.Delta * scale;
        return (self.Start, self.Start + scaledDelta);
    }

    public Vec2 FromTime(float t) => Start + (Delta * t);
    public bool OnRight(Vec2 point) => PerpDot(point) <= 0;
    public bool OnRight(Vec3 point) => OnRight(point.XY);
    public bool DifferentSides(Vec2 first, Vec2 second) => OnRight(first) != OnRight(second);
    public float floatTriArea(Vec2 point) => new Triangle2(Start, End, point).DoubleTriArea();

    public float PerpDot(Vec2 point)
    {
        Vec2 delta = Delta;
        return (delta.X * (point.Y - Start.Y)) - (delta.Y * (point.X - Start.X));
    }

    public bool Parallel(Seg2 seg, float epsilon = float.Epsilon)
    {
        Vec2 thisDelta = Delta;
        Vec2 otherDelta = seg.Delta;
        return (thisDelta.Y * otherDelta.X).ApproxEquals(thisDelta.X * otherDelta.Y, epsilon);
    }

    public bool Collinear(Seg2 seg)
    {
        return CollinearHelper(seg.Start, Start, End) && CollinearHelper(seg.End, Start, End);
    }

    public Vec2 ClosestPoint(Vec2 point)
    {
        Vec2 pointToStartDelta = Start - point;
        float t = -Delta.Dot(pointToStartDelta) / Delta.Dot(Delta);

        if (t <= 0)
            return Start;
        if (t >= 1)
            return End;
        return FromTime(t);
    }

    public bool TryIntersect(Seg2 seg, out float t)
    {
        // TODO: Is this right? The ordering of the last args?
        float areaStart = floatTriArea(seg.End);
        float areaEnd = floatTriArea(seg.Start);
        if (areaStart.DifferentSign(areaEnd))
        {
            float areaThisStart = seg.floatTriArea(Start);
            float areaThisEnd = seg.floatTriArea(End);
            if (areaThisStart.DifferentSign(areaThisEnd))
            {
                t = areaThisStart / (areaThisStart - areaThisEnd);
                return t >= 0 && t <= 1;
            }
        }

        t = default;
        return false;
    }

    public bool TryIntersect(Seg2 seg, out Vec2 point)
    {
        if (TryIntersect(seg, out float t))
        {
            point = FromTime(t);
            return true;
        }

        point = default;
        return false;
    }

    // Treats the current segment as a line and finds the intersection `t` along
    // this line with the target segment.
    public bool TryLineIntersect(Seg2 seg, out float t)
    {
        Vec2 thisDelta = Delta;
        Vec2 otherDelta = seg.Delta;
        float determinant = (-otherDelta.X * thisDelta.Y) + (thisDelta.X * otherDelta.Y);
        if (determinant.ApproxZero())
        {
            t = default;
            return false;
        }

        Vec2 startDelta = Start - seg.Start;
        t = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) / determinant;
        return true;
    }

    public bool TryLineIntersect(Seg2 seg, out Vec2 point)
    {
        if (TryLineIntersect(seg, out float t))
        {
            point = FromTime(t);
            return true;
        }

        point = default;
        return false;
    }

    private static bool CollinearHelper(Vec2 a, Vec2 b, Vec2 c)
    {
        return ((a.X * (b.Y - c.Y)) + (b.X * (c.Y - a.Y)) + (c.X * (a.Y - b.Y))).ApproxZero();
    }

    public override string ToString() => $"({Start}), ({End})";
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct Seg3(Vec3 Start, Vec3 End) :
    IAdditionOperators<Seg3, Vec3, Seg3>,
    ISubtractionOperators<Seg3, Vec3, Seg3>
{
    public Vec3 Delta => End - Start;
    public float Length => Start.Distance(End);
    public Box3 Box => new(this);
    public Ray3 Ray => new(Start, Delta.Unit);

    public static implicit operator Seg3(ValueTuple<Vec3, Vec3> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public void Deconstruct(out Vec3 start, out Vec3 end)
    {
        start = Start;
        end = End;
    }

    public static Seg3 operator +(Seg3 self, Vec3 other) => new(self.Start + other, self.End + other);
    public static Seg3 operator -(Seg3 self, Vec3 other) => new(self.Start - other, self.End - other);

    public Vec3 FromTime(float t) => Start + (Delta * t);

    public bool TryIntersect(in Box3 box, out float t)
    {
        // TODO

        t = default;
        return false;
    }

    public bool TryIntersect(in Box3 box, out Vec3 point)
    {
        if (TryIntersect(box, out float t))
        {
            point = FromTime(t);
            return true;
        }

        point = default;
        return false;
    }

    public override string ToString() => $"({Start}), ({End})";
}
