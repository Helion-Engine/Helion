// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GlmSharp;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Util.Extensions;

namespace Helion.Geometry.Vectors;

public class Vector4F
{
    public static readonly Vector4F Zero = new(0, 0, 0, 0);
    public static readonly Vector4F One = new(1, 1, 1, 1);

    public float X;
    public float Y;
    public float Z;
    public float W;

    public float U => X;
    public float V => Y;
    public Vec2F XY => new(X, Y);
    public Vec2F XZ => new(X, Z);
    public Vec3F XYZ => new(X, Y, Z);
    public Vec4I Int => new((int)X, (int)Y, (int)Z, (int)W);
    public Vec4D Double => new((double)X, (double)Y, (double)Z, (double)W);
    public Vec4Fixed FixedPoint => new(Fixed.From(X), Fixed.From(Y), Fixed.From(Z), Fixed.From(W));
    public Vec4F Struct => new(X, Y, Z, W);
    public IEnumerable<float> Values => GetEnumerableValues();

    public Vector4F(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public void Deconstruct(out float x, out float y, out float z, out float w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }

    public float this[int index]
    {
        get
        {
            return index switch
            {
                0 => X,
                1 => Y,
                2 => Z,
                3 => W,
                _ => throw new IndexOutOfRangeException()
            }
            ;
        }
        set
        {
            switch (index)
            {
                case 0:
                    X = value;
                    break;
                case 1:
                    Y = value;
                    break;
                case 2:
                    Z = value;
                    break;
                case 3:
                    W = value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }

    public static Vec4F operator -(Vector4F self) => new(-self.X, -self.Y, -self.Z, -self.W);
    public static Vec4F operator +(Vector4F self, Vec4F other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z, self.W + other.W);
    public static Vec4F operator +(Vector4F self, Vector4F other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z, self.W + other.W);
    public static Vec4F operator -(Vector4F self, Vec4F other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z, self.W - other.W);
    public static Vec4F operator -(Vector4F self, Vector4F other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z, self.W - other.W);
    public static Vec4F operator *(Vector4F self, Vec4F other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z, self.W * other.W);
    public static Vec4F operator *(Vector4F self, Vector4F other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z, self.W * other.W);
    public static Vec4F operator *(Vector4F self, float value) => new(self.X * value, self.Y * value, self.Z * value, self.W * value);
    public static Vec4F operator *(float value, Vector4F self) => new(self.X * value, self.Y * value, self.Z * value, self.W * value);
    public static Vec4F operator /(Vector4F self, Vec4F other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z, self.W / other.W);
    public static Vec4F operator /(Vector4F self, Vector4F other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z, self.W / other.W);
    public static Vec4F operator /(Vector4F self, float value) => new(self.X / value, self.Y / value, self.Z / value, self.W / value);

    public Vec4F WithX(float x) => new(x, Y, Z, W);
    public Vec4F WithY(float y) => new(X, y, Z, W);
    public Vec4F WithZ(float z) => new(X, Y, z, W);
    public Vec4F WithW(float w) => new(X, Y, Z, w);
    public bool IsApprox(Vec4F other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y) && Z.ApproxEquals(other.Z) && W.ApproxEquals(other.W);
    public bool IsApprox(Vector4F other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y) && Z.ApproxEquals(other.Z) && W.ApproxEquals(other.W);

    public Vec4F Abs() => new(X.Abs(), Y.Abs(), Z.Abs(), W.Abs());
    public Vec4F Floor() => new(X.Floor(), Y.Floor(), Z.Floor(), W.Floor());
    public Vec4F Ceiling() => new(X.Ceiling(), Y.Ceiling(), Z.Ceiling(), W.Ceiling());
    public Vec4F Unit() => this / Length();
    public void Normalize()
    {
        float len = Length();
        X /= len;
        Y /= len;
        Z /= len;
        W /= len;
    }
    public float LengthSquared() => (X * X) + (Y * Y) + (Z * Z) + (W * W);
    public Vec4F Inverse() => new(1 / X, 1 / Y, 1 / Z, 1 / W);
    public float Length() => MathF.Sqrt(LengthSquared());
    public float DistanceSquared(Vec4F other) => (this - other).LengthSquared();
    public float DistanceSquared(Vector4F other) => (this - other).LengthSquared();
    public float Distance(Vec4F other) => (this - other).Length();
    public float Distance(Vector4F other) => (this - other).Length();
    public Vec4F Interpolate(Vec4F end, float t) => this + (t * (end - this));
    public Vec4F Interpolate(Vector4F end, float t) => this + (t * (end - this));
    public float Dot(Vec4F other) => (X * other.X) + (Y * other.Y) + (Z * other.Z) + (W * other.W);
    public float Dot(Vector4F other) => (X * other.X) + (Y * other.Y) + (Z * other.Z) + (W * other.W);

    private IEnumerable<float> GetEnumerableValues()
    {
        yield return X;
        yield return Y;
        yield return Z;
        yield return W;
    }

    public override string ToString() => $"{X}, {Y}, {Z}, {W}";
    public override bool Equals(object? obj) => obj is Vector4F v && X == v.X && Y == v.Y && Z == v.Z && W == v.W;
    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
}

