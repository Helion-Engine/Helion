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

public class Vector4I
{
    public static readonly Vector4I Zero = new(0, 0, 0, 0);
    public static readonly Vector4I One = new(1, 1, 1, 1);

    public int X;
    public int Y;
    public int Z;
    public int W;

    public int U => X;
    public int V => Y;
    public Vec2I XY => new(X, Y);
    public Vec2I XZ => new(X, Z);
    public Vec3I XYZ => new(X, Y, Z);
    public Vec4F Float => new((float)X, (float)Y, (float)Z, (float)W);
    public Vec4D Double => new((double)X, (double)Y, (double)Z, (double)W);
    public Vec4Fixed FixedPoint => new(Fixed.From(X), Fixed.From(Y), Fixed.From(Z), Fixed.From(W));
    public Vec4I Struct => new(X, Y, Z, W);
    public IEnumerable<int> Values => GetEnumerableValues();

    public Vector4I(int x, int y, int z, int w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public void Deconstruct(out int x, out int y, out int z, out int w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }

    public int this[int index]
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

    public static Vec4I operator -(Vector4I self) => new(-self.X, -self.Y, -self.Z, -self.W);
    public static Vec4I operator +(Vector4I self, Vec4I other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z, self.W + other.W);
    public static Vec4I operator +(Vector4I self, Vector4I other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z, self.W + other.W);
    public static Vec4I operator -(Vector4I self, Vec4I other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z, self.W - other.W);
    public static Vec4I operator -(Vector4I self, Vector4I other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z, self.W - other.W);
    public static Vec4I operator *(Vector4I self, Vec4I other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z, self.W * other.W);
    public static Vec4I operator *(Vector4I self, Vector4I other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z, self.W * other.W);
    public static Vec4I operator *(Vector4I self, int value) => new(self.X * value, self.Y * value, self.Z * value, self.W * value);
    public static Vec4I operator *(int value, Vector4I self) => new(self.X * value, self.Y * value, self.Z * value, self.W * value);
    public static Vec4I operator /(Vector4I self, Vec4I other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z, self.W / other.W);
    public static Vec4I operator /(Vector4I self, Vector4I other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z, self.W / other.W);
    public static Vec4I operator /(Vector4I self, int value) => new(self.X / value, self.Y / value, self.Z / value, self.W / value);

    public Vec4I WithX(int x) => new(x, Y, Z, W);
    public Vec4I WithY(int y) => new(X, y, Z, W);
    public Vec4I WithZ(int z) => new(X, Y, z, W);
    public Vec4I WithW(int w) => new(X, Y, Z, w);

    public Vec4I Abs() => new(X.Abs(), Y.Abs(), Z.Abs(), W.Abs());
    public int Dot(Vec4I other) => (X * other.X) + (Y * other.Y) + (Z * other.Z) + (W * other.W);
    public int Dot(Vector4I other) => (X * other.X) + (Y * other.Y) + (Z * other.Z) + (W * other.W);

    private IEnumerable<int> GetEnumerableValues()
    {
        yield return X;
        yield return Y;
        yield return Z;
        yield return W;
    }

    public override string ToString() => $"{X}, {Y}, {Z}, {W}";
    public override bool Equals(object? obj) => obj is Vector4I v && X == v.X && Y == v.Y && Z == v.Z && W == v.W;
    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
}

