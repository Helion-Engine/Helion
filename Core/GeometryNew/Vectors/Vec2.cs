using System;
using System.Runtime.InteropServices;
using GlmSharp;
using Helion.GeometryNew.Boxes;
using Helion.GeometryNew.Segments;
using Helion.Util.Extensions;

namespace Helion.GeometryNew.Vectors;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Vec2
{
    public float X;
    public float Y;

    public float U => X;
    public float V => Y;
    public Vec2i Int => ((int)X, (int)Y);
    public vec2 GlmVector => new(X, Y);
    public Box2 Box => ((0, 0), (X, Y));
    public Vec2 Abs => (X.Abs(), Y.Abs());
    public Vec2 Floor => (X.Floor(), Y.Floor());
    public Vec2 Ceil => (X.Ceiling(), Y.Ceiling());
    public Vec2 Unit => this / Length;
    public float Length => MathF.Sqrt(LengthSquared);
    public float LengthSquared => (X * X) + (Y * Y);
    public Vec2 Inverse => new(1.0f / X, 1.0f / Y);
    
    public Vec2(float x, float y)
    {
        X = x;
        Y = y;
    }
    
    public static implicit operator Vec2(ValueTuple<float, float> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public void Deconstruct(out float x, out float y)
    {
        x = X;
        y = Y;
    }
    
    public static Vec2 operator -(Vec2 self) => new(-self.X, -self.Y);
    public static Vec2 operator +(Vec2 self, Vec2 other) => new(self.X + other.X, self.Y + other.Y);
    public static Vec2 operator -(Vec2 self, Vec2 other) => new(self.X - other.X, self.Y - other.Y);
    public static Vec2 operator *(Vec2 self, Vec2 other) => new(self.X * other.X, self.Y * other.Y);
    public static Vec2 operator *(Vec2 self, float value) => new(self.X * value, self.Y * value);
    public static Vec2 operator *(float value, Vec2 self) => new(self.X * value, self.Y * value);
    public static Vec2 operator /(Vec2 self, Vec2 other) => new(self.X / other.X, self.Y / other.Y);
    public static Vec2 operator /(Vec2 self, float value) => new(self.X / value, self.Y / value);
    public static bool operator ==(Vec2 self, Vec2 other) => self.X == other.X && self.Y == other.Y;
    public static bool operator !=(Vec2 self, Vec2 other) => !(self == other);

    public Vec3 Z(float z) => (X, Y, z);
    public Vec2 Min(Vec2 vec) => (X.Min(vec.X), Y.Min(vec.Y));
    public Vec2 Max(Vec2 vec) => (X.Max(vec.X), Y.Max(vec.Y));
    public bool Approx(Vec2 other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y);
    public float DistanceSquared(Vec2 other) => (this - other).LengthSquared;
    public float Distance(Vec2 other) => (this - other).Length;
    public Vec2 Lerp(Vec2 end, float t) => this + (t * (end - this));
    public float Dot(Vec2 other) => (X * other.X) + (Y * other.Y);
    public float Component(Vec2 onto) => Dot(onto) / onto.Length;
    public Vec2 Projection(Vec2 onto) => Dot(onto) / onto.LengthSquared * onto;
    public Vec2 Right90 => (Y, -X);
    public Vec2 Left90 => (-Y, X);
    public float Angle(in Vec2 other) => MathF.Atan2(other.Y - Y, other.X - X);
    public float Angle(in Vec3 other) => MathF.Atan2(other.Y - Y, other.X - X);
    public Rotation Rotation(Vec2 second, Vec2 third, float epsilon = 0.0001f) => new Seg2(this, second).ToSide(third, epsilon);
    
    public void Normalize()
    {
        this /= Length;
    }

    public Vec2 Rotate(float radians)
    {
        float sin = MathF.Sin(radians);
        float cos = MathF.Cos(radians);
        return ((X * cos) - (Y * sin), (X * sin) + (Y * cos));
    }

    public override string ToString() => $"{X}, {Y}";
    public override bool Equals(object? obj) => obj is Vec2 v && this == v;
    public override int GetHashCode() => HashCode.Combine(X, Y);
}