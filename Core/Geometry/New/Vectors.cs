using GlmSharp;
using Helion.Util.Extensions;
using System;
using System.Runtime.InteropServices;

namespace Helion.Geometry.New;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Vec2i
{
    public int X;
    public int Y;

    public int Width => X;
    public int Height => Y;
    public int Area => Width * Height;
    public float Aspect => (float)Width / (float)Height;
    public Vec2i RotateRight90 => (Y, -X);
    public Vec2i RotateLeft90 => (-Y, X);
    public Vec2 Float => (X, Y);

    public Vec2i(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static implicit operator Vec2i(ValueTuple<int, int> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    public static Vec2i operator -(Vec2i self) => new(-self.X, -self.Y);
    public static Vec2i operator +(Vec2i self, Vec2i other) => new(self.X + other.X, self.Y + other.Y);
    public static Vec2i operator -(Vec2i self, Vec2i other) => new(self.X - other.X, self.Y - other.Y);
    public static Vec2i operator *(Vec2i self, int scale) => new(self.X * scale, self.Y * scale);
    public static Vec2i operator *(Vec2i self, Vec2i other) => new(self.X * other.X, self.Y * other.Y);
    public static bool operator <(Vec2i self, Vec2i other) => self.X < other.X && self.Y < other.Y;
    public static bool operator <=(Vec2i self, Vec2i other) => self.X <= other.X && self.Y <= other.Y;
    public static bool operator >(Vec2i self, Vec2i other) => self.X > other.X && self.Y > other.Y;
    public static bool operator >=(Vec2i self, Vec2i other) => self.X >= other.X && self.Y >= other.Y;
    public static bool operator ==(Vec2i self, Vec2i other) => self.X == other.X && self.Y == other.Y;
    public static bool operator !=(Vec2i self, Vec2i other) => !(self == other);

    public Vec2i Min(Vec2i other) => (Math.Min(X, other.X), Math.Min(Y, other.Y));
    public Vec2i Max(Vec2i other) => (Math.Max(X, other.X), Math.Max(Y, other.Y));

    public override string ToString() => $"{X}, {Y}";
    public override bool Equals(object? obj) => obj is Vec2 v && Equals(v);
    public override int GetHashCode() => HashCode.Combine(X, Y);
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Vec2 : 
    IAdditionOperators<Vec2, Vec2, Vec2>,
    ISubtractionOperators<Vec2, Vec2, Vec2>,
    IMultiplyOperators<Vec2, Vec2, Vec2>,
    IMultiplyOperators<Vec2, float, Vec2>,
    IDivisionOperators<Vec2, Vec2, Vec2>,
    IDivisionOperators<Vec2, float, Vec2>,
    IEqualityOperators<Vec2, Vec2>,
    IUnaryNegationOperators<Vec2, Vec2>
{
    public static readonly Vec2 Zero = (0.0f, 0.0f);
    public static readonly Vec2 One = (1.0f, 1.0f);

    public float X;
    public float Y;

    public float U => X;
    public float V => Y;
    public float Width => X;
    public float Height => Y;
    public float Area => Width * Height;
    public float LengthSquared => (X * X) + (Y * Y);
    public float Length => MathF.Sqrt(LengthSquared);
    public Vec2 Unit => this / Length;
    public Vec2 RotateRight90 => (Y, -X);
    public Vec2 RotateLeft90 => (-Y, X);
    public Vec2 Inverse => (1.0f / X, 1.0f / Y);
    public Vec2 Floor => (MathF.Floor(X), MathF.Floor(Y));
    public Vec2 Ceil => (MathF.Ceiling(X), MathF.Ceiling(Y));
    public vec2 Glm => new(X, Y);
    public Vec2i Int => ((int)X, (int)Y);

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
    public static bool operator <(Vec2 self, Vec2 other) => self.X < other.X && self.Y < other.Y;
    public static bool operator <=(Vec2 self, Vec2 other) => self.X <= other.X && self.Y <= other.Y;
    public static bool operator >(Vec2 self, Vec2 other) => self.X > other.X && self.Y > other.Y;
    public static bool operator >=(Vec2 self, Vec2 other) => self.X >= other.X && self.Y >= other.Y;
    public static bool operator ==(Vec2 self, Vec2 other) => self.X == other.X && self.Y == other.Y;
    public static bool operator !=(Vec2 self, Vec2 other) => !(self == other);

    public Vec3 Z(float z) => (X, Y, z);
    public Vec2 Min(Vec2 other) => (MathF.Min(X, other.X), MathF.Min(Y, other.Y));
    public Vec2 Max(Vec2 other) => (MathF.Max(X, other.X), MathF.Max(Y, other.Y));
    public float Dot(Vec2 other) => (X * other.X) + (Y * other.Y);
    public float Distance(Vec2 other) => (other - this).Length;
    public float DistanceSquared(Vec2 other) => (other - this).LengthSquared;
    public void Normalize() => this /= Length;
    public float Component(Vec2 onto) => Dot(onto) / onto.Length;
    public Vec2 Projection(Vec2 onto) => Dot(onto) / onto.LengthSquared * onto;
    public float Angle(Vec2 other) => MathF.Atan2(other.Y - Y, other.X - X);
    public float Angle(Vec3 other) => MathF.Atan2(other.Y - Y, other.X - X);

    public Vec2 Rotate(float radians)
    {
        float sin = MathF.Sin(radians);
        float cos = MathF.Cos(radians);
        return new((X * cos) - (Y * sin), (X * sin) + (Y * cos));
    }

    public bool Equals(Vec2 other) => X == other.X && Y == other.Y;
    public bool ApproxEquals(Vec2 other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y);
    public override bool Equals(object? obj) => obj is Vec2 v && Equals(v);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"{X}, {Y}";
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Vec3 :
    IAdditionOperators<Vec3, Vec3, Vec3>,
    ISubtractionOperators<Vec3, Vec3, Vec3>,
    IMultiplyOperators<Vec3, Vec3, Vec3>,
    IMultiplyOperators<Vec3, float, Vec3>,
    IDivisionOperators<Vec3, Vec3, Vec3>,
    IDivisionOperators<Vec3, float, Vec3>,
    IEqualityOperators<Vec3, Vec3>,
    IUnaryNegationOperators<Vec3, Vec3>
{
    public float X;
    public float Y;
    public float Z;

    public Vec2 XY => new(X, Y);
    public Vec2 XZ => new(X, Z);
    public Vec2 YZ => new(Y, Z);
    public Vec3 Unit => this / Length;
    public Vec3 Inverse => new(1.0f / X, 1.0f / Y, 1.0f / Z);
    public Vec3 Floor => (MathF.Floor(X), MathF.Floor(Y), MathF.Floor(Z));
    public Vec3 Ceil => (MathF.Ceiling(X), MathF.Ceiling(Y), MathF.Ceiling(Z));
    public float LengthSquared => (X * X) + (Y * Y) + (Z * Z);
    public float Length => MathF.Sqrt(LengthSquared);

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
    public static bool operator <(Vec3 self, Vec3 other) => self.X < other.X && self.Y < other.Y && self.Z < other.Z;
    public static bool operator <=(Vec3 self, Vec3 other) => self.X <= other.X && self.Y <= other.Y && self.Z <= other.Z;
    public static bool operator >(Vec3 self, Vec3 other) => self.X > other.X && self.Y > other.Y && self.Z > other.Z;
    public static bool operator >=(Vec3 self, Vec3 other) => self.X >= other.X && self.Y >= other.Y && self.Z >= other.Z;
    public static bool operator ==(Vec3 self, Vec3 other) => self.X == other.X && self.Y == other.Y && self.Z == other.Z;
    public static bool operator !=(Vec3 self, Vec3 other) => !(self == other);

    public Vec3 Min(Vec3 other) => (MathF.Min(X, other.X), MathF.Min(Y, other.Y), MathF.Min(Z, other.Z));
    public Vec3 Max(Vec3 other) => (MathF.Max(X, other.X), MathF.Max(Y, other.Y), MathF.Min(Z, other.Z));
    public void Normalize() => this /= Length;
    public float DistanceSquared(Vec3 other) => (this - other).LengthSquared;
    public float Distance(Vec3 other) => (this - other).Length;
    public float Dot(Vec3 other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
    public Vec3 Cross(Vec3 v) => ((Y * v.Z) - (Z * v.Y), (Z * v.X) - (X * v.Z), (X * v.Y) - (Y * v.X));

    public bool Equals(Vec3 other) => X == other.X && Y == other.Y && Z == other.Z;
    public bool ApproxEquals(Vec3 other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y) && Z.ApproxEquals(other.Z);
    public override bool Equals(object? obj) => obj is Vec3 v && X == v.X && Y == v.Y && Z == v.Z;
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => $"{X}, {Y}, {Z}";
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct Vec4(float X, float Y, float Z, float W)
{
    public static implicit operator Vec4(ValueTuple<float, float, float, float> tuple)
    {
        return new(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
    }
}
