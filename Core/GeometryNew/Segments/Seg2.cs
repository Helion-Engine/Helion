using System;
using System.Runtime.InteropServices;
using Helion.GeometryNew.Boxes;
using Helion.GeometryNew.Vectors;
using Helion.Util.Extensions;

namespace Helion.GeometryNew.Segments;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Seg2
{
    public Vec2 Start;
    public Vec2 End;

    public Vec2 Delta => End - Start;
    public Box2 Box => (Start.Min(End), Start.Max(End));
    public float Length => Start.Distance(End);
    public bool IsAxisAligned => Start.X.ApproxEquals(End.X) || Start.Y.ApproxEquals(End.Y);
    
    public Seg2(Vec2 start, Vec2 end)
    {
        Start = start;
        End = end;
    }

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
    public static bool operator ==(Seg2 self, Seg2 other) => self.Start == other.Start && self.End == other.End;
    public static bool operator !=(Seg2 self, Seg2 other) => !(self == other);
    
    private static bool CollinearHelper(float aX, float aY, float bX, float bY, float cX, float cY)
    {
        return ((aX * (bY - cY)) + (bX * (cY - aY)) + (cX * (aY - bY))).ApproxZero();
    }
    
    private static float DoubleTriArea(float aX, float aY, float bX, float bY, float cX, float cY)
    {
        return ((aX - cX) * (bY - cY)) - ((aY - cY) * (bX - cX));
    }
    
    public Vec2 FromTime(float t) => Start + (Delta * t);
    public bool SameDirection(Seg2 seg) => SameDirection(seg.Delta);
    public float PerpDot(Vec2 point) => (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));
    public double PerpDot(Vec3 point) => PerpDot(point.XY);
    public bool OnRight(Vec2 point) => PerpDot(point) <= 0;
    public bool OnRight(Vec3 point) => PerpDot(point.XY) <= 0;
    public bool OnRight(Seg2 seg) => OnRight(seg.Start) && OnRight(seg.End);
    public bool DifferentSides(Vec2 first, Vec2 second) => OnRight(first) != OnRight(second);
    public bool DifferentSides(Seg2 seg) => OnRight(seg.Start) != OnRight(seg.End);
    
    public bool SameDirection(Vec2 delta)
    {
        Vec2 thisDelta = Delta;
        return !thisDelta.X.DifferentSign(delta.X) && !thisDelta.Y.DifferentSign(delta.Y);
    }
    
    public Rotation ToSide(Vec2 point, float epsilon = 0.0001f)
    {
        float value = PerpDot(point);
        bool approxZero = value.ApproxZero(epsilon);
        return approxZero ? Rotation.On : (value < 0 ? Rotation.Right : Rotation.Left);
    }
    
    public float ToTime(Vec2 point)
    {
        if (Start.X.ApproxEquals(End.X))
            return (point.Y - Start.Y) / (End.Y - Start.Y);
        return (point.X - Start.X) / (End.X - Start.X);
    }
    
    public bool Parallel(Seg2 seg, float epsilon = 0.0001f)
    {
        return (Delta.Y * seg.Delta.X).ApproxEquals(Delta.X * seg.Delta.Y, epsilon);
    }
    
    public bool Collinear(Seg2 seg)
    {
        return CollinearHelper(seg.Start.X, seg.Start.Y, Start.X, Start.Y, End.X, End.Y) &&
               CollinearHelper(seg.End.X, seg.End.Y, Start.X, Start.Y, End.X, End.Y);
    }
    
    public bool Intersects(Seg2 other) => Intersection(other, out _);
    
    public bool Intersection(Seg2 seg, out float tLhs)
    {
        float areaStart = DoubleTriArea(Start.X, Start.Y, End.X, End.Y, seg.End.X, seg.End.Y);
        float areaEnd = DoubleTriArea(Start.X, Start.Y, End.X, End.Y, seg.Start.X, seg.Start.Y);
    
        if (areaStart.DifferentSign(areaEnd))
        {
            float areaThisStart = DoubleTriArea(seg.Start.X, seg.Start.Y, seg.End.X, seg.End.Y, Start.X, Start.Y);
            float areaThisEnd = DoubleTriArea(seg.Start.X, seg.Start.Y, seg.End.X, seg.End.Y, End.X, End.Y);
    
            if (areaThisStart.DifferentSign(areaThisEnd))
            {
                tLhs = areaThisStart / (areaThisStart - areaThisEnd);
                return tLhs >= 0 && tLhs <= 1;
            }
        }
    
        tLhs = default;
        return false;
    }
    
    public bool IntersectionAsLine(Seg2 seg, out float tLhs)
    {
        float determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
        if (determinant.ApproxZero())
        {
            tLhs = default;
            return false;
        }
    
        Vec2 startDelta = Start - seg.Start;
        tLhs = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) / determinant;
        return true;
    }
    
    public bool IntersectionAsLine(Seg2 seg, out float tLhs, out float tRhs)
    {
        float determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
        if (determinant.ApproxZero())
        {
            tLhs = default;
            tRhs = default;
            return false;
        }
    
        Vec2 startDelta = Start - seg.Start;
        float inverseDeterminant = 1.0f / determinant;
        tLhs = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) * inverseDeterminant;
        tRhs = ((-Delta.Y * startDelta.X) + (Delta.X * startDelta.Y)) * inverseDeterminant;
        return true;
    }
    
    public Vec2 ClosestPoint(Vec2 point)
    {
        float t = -Delta.Dot(Start - point) / Delta.Dot(Delta);
        if (t <= 0)
            return Start;
        if (t >= 1)
            return End;
        return FromTime(t);
    }
    
    public bool Intersects(Box2 box)
    {
        if (!box.Intersects(Box))
            return false;
        if (Start.X.ApproxEquals(End.X))
            return box.Min.X < Start.X && Start.X < box.Max.X;
        if (Start.Y.ApproxEquals(End.Y))
            return box.Min.Y < Start.Y && Start.Y < box.Max.Y;
        return ((Start.X < End.X) ^ (Start.Y < End.Y)) ?
            DifferentSides(box.BottomLeft, box.TopRight) :
            DifferentSides(box.TopLeft, box.BottomRight);
    }
    
    public override string ToString() => $"({Start}), ({End})";
    public override bool Equals(object? obj) => obj is Seg2 seg && Start == seg.Start && End == seg.End;
    public override int GetHashCode() => HashCode.Combine(Start.GetHashCode(), End.GetHashCode());
}