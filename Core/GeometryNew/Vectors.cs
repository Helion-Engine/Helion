using GlmSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Helion.GeometryNew;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Vec2i
{
    public int X;
    public int Y;

    public int Width => X;
    public int Height => Y;
    public int Area => Width * Height;
    public float Aspect => (float)Width / (float)Height;

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
    public static bool operator ==(Vec2i self, Vec2i other) => self.X == other.X && self.Y == other.Y;
    public static bool operator !=(Vec2i self, Vec2i other) => !(self == other);

    public override string ToString() => $"{X}, {Y}";
    public override bool Equals(object? obj) => obj is Vec2f v && Equals(v);
    public override int GetHashCode() => HashCode.Combine(X, Y);
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Vec2f : 
    IAdditionOperators<Vec2f, Vec2f, Vec2f>,
    ISubtractionOperators<Vec2f, Vec2f, Vec2f>,
    IMultiplyOperators<Vec2f, Vec2f, Vec2f>,
    IMultiplyOperators<Vec2f, float, Vec2f>,
    IDivisionOperators<Vec2f, Vec2f, Vec2f>,
    IDivisionOperators<Vec2f, float, Vec2f>,
    IEqualityOperators<Vec2f, Vec2f>,
    IUnaryNegationOperators<Vec2f, Vec2f>
{
    public static readonly Vec2f Zero = (0.0f, 0.0f);
    public static readonly Vec2f One = (1.0f, 1.0f);

    public float X;
    public float Y;

    public float U => X;
    public float V => Y;
    public float Width => X;
    public float Height => Y;
    public float Area => Width * Height;
    public Vec2f Unit => this / Length;
    public float LengthSquared => (X * X) + (Y * Y);
    public float Length => MathF.Sqrt(LengthSquared);
    public Vec2f Inverse => new(1.0f / X, 1.0f / Y);
    public vec2 Glm => new(X, Y);
    public Vec2d Double => (X, Y);

    public Vec2f(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static implicit operator Vec2f(ValueTuple<float, float> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public void Deconstruct(out float x, out float y)
    {
        x = X;
        y = Y;
    }

    public static Vec2f operator -(Vec2f self) => new(-self.X, -self.Y);
    public static Vec2f operator +(Vec2f self, Vec2f other) => new(self.X + other.X, self.Y + other.Y);
    public static Vec2f operator -(Vec2f self, Vec2f other) => new(self.X - other.X, self.Y - other.Y);
    public static Vec2f operator *(Vec2f self, Vec2f other) => new(self.X * other.X, self.Y * other.Y);
    public static Vec2f operator *(Vec2f self, float value) => new(self.X * value, self.Y * value);
    public static Vec2f operator *(float value, Vec2f self) => new(self.X * value, self.Y * value);
    public static Vec2f operator /(Vec2f self, Vec2f other) => new(self.X / other.X, self.Y / other.Y);
    public static Vec2f operator /(Vec2f self, float value) => new(self.X / value, self.Y / value);
    public static bool operator ==(Vec2f self, Vec2f other) => self.X == other.X && self.Y == other.Y;
    public static bool operator !=(Vec2f self, Vec2f other) => !(self == other);

    public Vec3f Z(float z) => (X, Y, z);
    public Vec2f Min(Vec2f other) => (MathF.Min(X, other.X), MathF.Min(Y, other.Y));
    public Vec2f Max(Vec2f other) => (MathF.Max(X, other.X), MathF.Max(Y, other.Y));
    public float Dot(Vec2f other) => (X * other.X) + (Y * other.Y);
    public float Distance(Vec2f other) => (other - this).Length;
    public float DistanceSquared(Vec2f other) => (other - this).LengthSquared;
    public void Normalize() => this /= Length;
    public Vec2f Lerp(Vec2f end, float t) => this + ((end - this) * t);
    public float Angle(Vec2f other) => MathF.Atan2(other.Y - Y, other.X - X);
    public float Angle(Vec3f other) => MathF.Atan2(other.Y - Y, other.X - X);
    public bool Equals(Vec2f other) => X == other.X && Y == other.Y;

    public override string ToString() => $"{X}, {Y}";
    public override bool Equals(object? obj) => obj is Vec2f v && Equals(v);
    public override int GetHashCode() => HashCode.Combine(X, Y);
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct Vec2d :
    IAdditionOperators<Vec2d, Vec2d, Vec2d>,
    ISubtractionOperators<Vec2d, Vec2d, Vec2d>,
    IMultiplyOperators<Vec2d, Vec2d, Vec2d>,
    IMultiplyOperators<Vec2d, double, Vec2d>,
    IDivisionOperators<Vec2d, Vec2d, Vec2d>,
    IDivisionOperators<Vec2d, double, Vec2d>,
    IEqualityOperators<Vec2d, Vec2d>,
    IUnaryNegationOperators<Vec2d, Vec2d>
{
    public static readonly Vec2d Zero = (0.0, 0.0);
    public static readonly Vec2d One = (1.0, 1.0);

    public double X;
    public double Y;

    public double U => X;
    public double V => Y;
    public double Width => X;
    public double Height => Y;
    public double Area => Width * Height;
    public Vec2d Unit => this / Length;
    public double LengthSquared => (X * X) + (Y * Y);
    public double Length => Math.Sqrt(LengthSquared);
    public Vec2d Inverse => new(1.0f / X, 1.0f / Y);
    public Vec2f Float => ((float)X, (float)Y);

    public Vec2d(double x, double y)
    {
        X = x;
        Y = y;
    }

    public static implicit operator Vec2d(ValueTuple<double, double> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public void Deconstruct(out double x, out double y)
    {
        x = X;
        y = Y;
    }

    public static Vec2d operator -(Vec2d self) => new(-self.X, -self.Y);
    public static Vec2d operator +(Vec2d self, Vec2d other) => new(self.X + other.X, self.Y + other.Y);
    public static Vec2d operator -(Vec2d self, Vec2d other) => new(self.X - other.X, self.Y - other.Y);
    public static Vec2d operator *(Vec2d self, Vec2d other) => new(self.X * other.X, self.Y * other.Y);
    public static Vec2d operator *(Vec2d self, double value) => new(self.X * value, self.Y * value);
    public static Vec2d operator *(double value, Vec2d self) => new(self.X * value, self.Y * value);
    public static Vec2d operator /(Vec2d self, Vec2d other) => new(self.X / other.X, self.Y / other.Y);
    public static Vec2d operator /(Vec2d self, double value) => new(self.X / value, self.Y / value);
    public static bool operator ==(Vec2d self, Vec2d other) => self.X == other.X && self.Y == other.Y;
    public static bool operator !=(Vec2d self, Vec2d other) => !(self == other);

    public Vec3d Z(double z) => (X, Y, z);
    public Vec2d Min(Vec2d other) => (Math.Min(X, other.X), Math.Min(Y, other.Y));
    public Vec2d Max(Vec2d other) => (Math.Max(X, other.X), Math.Max(Y, other.Y));
    public double Dot(Vec2d other) => (X * other.X) + (Y * other.Y);
    public void Normalize() => this /= Length;
    public double DistanceSquared(Vec2d other) => (this - other).LengthSquared;
    public double Distance(Vec2d other) => (this - other).Length;
    public double Angle(Vec2d other) => Math.Atan2(other.Y - Y, other.X - X);
    public double Angle(Vec3d other) => Math.Atan2(other.Y - Y, other.X - X);
    public bool Equals(Vec2d other) => X == other.X && Y == other.Y;

    public override string ToString() => $"{X}, {Y}";
    public override bool Equals(object? obj) => obj is Vec2d v && X == v.X && Y == v.Y;
    public override int GetHashCode() => HashCode.Combine(X, Y);
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Vec3f :
    IAdditionOperators<Vec3f, Vec3f, Vec3f>,
    ISubtractionOperators<Vec3f, Vec3f, Vec3f>,
    IMultiplyOperators<Vec3f, Vec3f, Vec3f>,
    IMultiplyOperators<Vec3f, float, Vec3f>,
    IDivisionOperators<Vec3f, Vec3f, Vec3f>,
    IDivisionOperators<Vec3f, float, Vec3f>,
    IEqualityOperators<Vec3f, Vec3f>,
    IUnaryNegationOperators<Vec3f, Vec3f>
{
    public float X;
    public float Y;
    public float Z;

    public Vec2f XY => new(X, Y);
    public Vec2f XZ => new(X, Z);
    public Vec2f YZ => new(Y, Z);
    public Vec3f Unit => this / Length;
    public Vec3f Inverse => new(1.0f / X, 1.0f / Y, 1.0f / Z);
    public float LengthSquared => (X * X) + (Y * Y) + (Z * Z);
    public float Length => MathF.Sqrt(LengthSquared);

    public Vec3f(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static implicit operator Vec3f(ValueTuple<float, float, float> tuple)
    {
        return new(tuple.Item1, tuple.Item2, tuple.Item3);
    }

    public void Deconstruct(out float x, out float y, out float z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    public static Vec3f operator -(Vec3f self) => new(-self.X, -self.Y, -self.Z);
    public static Vec3f operator +(Vec3f self, Vec3f other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
    public static Vec3f operator -(Vec3f self, Vec3f other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
    public static Vec3f operator *(Vec3f self, Vec3f other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
    public static Vec3f operator *(Vec3f self, float value) => new(self.X * value, self.Y * value, self.Z * value);
    public static Vec3f operator *(float value, Vec3f self) => new(self.X * value, self.Y * value, self.Z * value);
    public static Vec3f operator /(Vec3f self, Vec3f other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
    public static Vec3f operator /(Vec3f self, float value) => new(self.X / value, self.Y / value, self.Z / value);
    public static bool operator ==(Vec3f self, Vec3f other) => self.X == other.X && self.Y == other.Y && self.Z == other.Z;
    public static bool operator !=(Vec3f self, Vec3f other) => !(self == other);

    public Vec3f Min(Vec3f other) => (MathF.Min(X, other.X), MathF.Min(Y, other.Y), MathF.Min(Z, other.Z));
    public Vec3f Max(Vec3f other) => (MathF.Max(X, other.X), MathF.Max(Y, other.Y), MathF.Min(Z, other.Z));
    public void Normalize() => this /= Length;
    public float DistanceSquared(Vec3f other) => (this - other).LengthSquared;
    public float Distance(Vec3f other) => (this - other).Length;
    public Vec3f Lerp(Vec3f end, float t) => this + (t * (end - this));
    public float Dot(Vec3f other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
    public bool Equals(Vec3f other) => X == other.X && Y == other.Y;

    public override string ToString() => $"{X}, {Y}, {Z}";
    public override bool Equals(object? obj) => obj is Vec3f v && X == v.X && Y == v.Y && Z == v.Z;
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct Vec3d :
    IAdditionOperators<Vec3d, Vec3d, Vec3d>,
    ISubtractionOperators<Vec3d, Vec3d, Vec3d>,
    IMultiplyOperators<Vec3d, Vec3d, Vec3d>,
    IMultiplyOperators<Vec3d, double, Vec3d>,
    IDivisionOperators<Vec3d, Vec3d, Vec3d>,
    IDivisionOperators<Vec3d, double, Vec3d>,
    IEqualityOperators<Vec3d, Vec3d>,
    IUnaryNegationOperators<Vec3d, Vec3d>
{
    public double X;
    public double Y;
    public double Z;

    public Vec2d XY => new(X, Y);
    public Vec2d XZ => new(X, Z);
    public Vec2d YZ => new(Y, Z);
    public Vec3d Unit => this / Length;
    public Vec3d Inverse => new(1.0 / X, 1.0 / Y, 1.0 / Z);
    public double LengthSquared => (X * X) + (Y * Y) + (Z * Z);
    public double Length => Math.Sqrt(LengthSquared);

    public Vec3d(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static implicit operator Vec3d(ValueTuple<double, double, double> tuple)
    {
        return new(tuple.Item1, tuple.Item2, tuple.Item3);
    }

    public void Deconstruct(out double x, out double y, out double z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    public static Vec3d operator -(Vec3d self) => new(-self.X, -self.Y, -self.Z);
    public static Vec3d operator +(Vec3d self, Vec3d other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
    public static Vec3d operator -(Vec3d self, Vec3d other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
    public static Vec3d operator *(Vec3d self, Vec3d other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
    public static Vec3d operator *(Vec3d self, double value) => new(self.X * value, self.Y * value, self.Z * value);
    public static Vec3d operator *(double value, Vec3d self) => new(self.X * value, self.Y * value, self.Z * value);
    public static Vec3d operator /(Vec3d self, Vec3d other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
    public static Vec3d operator /(Vec3d self, double value) => new(self.X / value, self.Y / value, self.Z / value);
    public static bool operator ==(Vec3d self, Vec3d other) => self.X == other.X && self.Y == other.Y && self.Z == other.Z;
    public static bool operator !=(Vec3d self, Vec3d other) => !(self == other);

    public Vec3d Min(Vec3d other) => (Math.Min(X, other.X), Math.Min(Y, other.Y), Math.Min(Z, other.Z));
    public Vec3d Max(Vec3d other) => (Math.Max(X, other.X), Math.Max(Y, other.Y), Math.Min(Z, other.Z));
    public void Normalize() => this /= Length;
    public double DistanceSquared(Vec3d other) => (this - other).LengthSquared;
    public double Distance(Vec3d other) => (this - other).Length;
    public Vec3d Lerp(Vec3d end, double t) => this + (t * (end - this));
    public double Dot(Vec3d other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
    public bool Equals(Vec3d other) => X == other.X && Y == other.Y;

    public override string ToString() => $"{X}, {Y}, {Z}";
    public override bool Equals(object? obj) => obj is Vec3d v && X == v.X && Y == v.Y && Z == v.Z;
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct Vec4f(float X, float Y, float Z, float W)
{
    public static implicit operator Vec4f(ValueTuple<float, float, float, float> tuple)
    {
        return new(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public record struct Vec4d(double X, double Y, double Z, double W)
{
    public static implicit operator Vec4d(ValueTuple<double, double, double, double> tuple)
    {
        return new(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
    }
}
