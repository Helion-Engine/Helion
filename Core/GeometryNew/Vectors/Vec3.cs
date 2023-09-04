using System;
using System.Runtime.InteropServices;
using GlmSharp;
using Helion.Util.Extensions;

namespace Helion.GeometryNew.Vectors;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Vec3
{
    public float X;
    public float Y;
    public float Z;

    public Vec2 XY => (X, Y);
    public Vec2 XZ => (X, Z);
    public vec3 GlmVector => new(X, Y, Z);
    public Vec3 Abs => (X.Abs(), Y.Abs(), Z.Abs());
    public Vec3 Floor => (X.Floor(), Y.Floor(), Z.Floor());
    public Vec3 Ceil => (X.Ceiling(), Y.Ceiling(), Z.Ceiling());
    public Vec3 Unit => this / Length;
    public float Length => MathF.Sqrt(LengthSquared);
    public float LengthSquared => (X * X) + (Y * Y) + (Z * Z);
    public Vec3 Inverse => new(1.0f / X, 1.0f / Y, 1.0f / Z);

    public Vec3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
    
    public static implicit operator Vec3(ValueTuple<float, float, float> tuple)
    {
        return new(tuple.Item1, tuple.Item2, tuple.Item3);
    }

    public void Deconstruct(out float x, out float y, out float z)
    {
        x = X;
        y = Y;
        z = Z;
    }
    
    public static Vec3 operator -(Vec3 self) => new(-self.X, -self.Y, -self.Z);
    public static Vec3 operator +(Vec3 self, Vec3 other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
    public static Vec3 operator -(Vec3 self, Vec3 other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
    public static Vec3 operator *(Vec3 self, Vec3 other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
    public static Vec3 operator *(Vec3 self, float value) => new(self.X * value, self.Y * value, self.Z * value);
    public static Vec3 operator *(float value, Vec3 self) => new(self.X * value, self.Y * value, self.Z * value);
    public static Vec3 operator /(Vec3 self, Vec3 other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
    public static Vec3 operator /(Vec3 self, float value) => new(self.X / value, self.Y / value, self.Z / value);
    public static bool operator ==(Vec3 self, Vec3 other) => self.X == other.X && self.Y == other.Y && self.Z == other.Z;
    public static bool operator !=(Vec3 self, Vec3 other) => !(self == other);
    
    public static Vec3 UnitSphere(float angle, float pitch)
    {
        float sinAngle = MathF.Sin(angle);
        float cosAngle = MathF.Cos(angle);
        float sinPitch = MathF.Sin(pitch);
        float cosPitch = MathF.Cos(pitch);
        return new(cosAngle * cosPitch, sinAngle * cosPitch, sinPitch);
    }
    
    public bool Approx(Vec3 other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y) && Z.ApproxEquals(other.Z);
    public float DistanceSquared(Vec3 other) => (this - other).LengthSquared;
    public float Distance(Vec3 other) => (this - other).Length;
    public Vec3 Lerp(Vec3 end, float t) => this + (t * (end - this));
    public float Dot(Vec3 other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
    public float Pitch(Vec3 other, float length) => MathF.Atan2(other.Z - Z, length);
    public float Angle(Vec3 other) => MathF.Atan2(other.Y - Y, other.X - X);
    
    public void Normalize()
    {
        this /= Length;
    }
    
    public override string ToString() => $"{X}, {Y}, {Z}";
    public override bool Equals(object? obj) => obj is Vec3 v && this == v;
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
}