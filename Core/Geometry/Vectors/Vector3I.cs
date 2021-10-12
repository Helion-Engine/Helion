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

public class Vector3I
{
    public static readonly Vector3I Zero = new(0, 0, 0);
    public static readonly Vector3I One = new(1, 1, 1);

    public int X;
    public int Y;
    public int Z;

    public int U => X;
    public int V => Y;
    public Vec2I XY => new(X, Y);
    public Vec2I XZ => new(X, Z);
    public Vec3F Float => new((float)X, (float)Y, (float)Z);
    public Vec3D Double => new((double)X, (double)Y, (double)Z);
    public Vec3Fixed FixedPoint => new(Fixed.From(X), Fixed.From(Y), Fixed.From(Z));
    public Box3I Box => new((0, 0, 0), (X, Y, Z));
    public Vec3I Struct => new(X, Y, Z);
    public IEnumerable<int> Values => GetEnumerableValues();

    public Vector3I(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public void Deconstruct(out int x, out int y, out int z)
    {
        x = X;
        y = Y;
        z = Z;
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
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }

    public static Vec3I operator -(Vector3I self) => new(-self.X, -self.Y, -self.Z);
    public static Vec3I operator +(Vector3I self, Vec3I other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
    public static Vec3I operator +(Vector3I self, Vector3I other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
    public static Vec3I operator -(Vector3I self, Vec3I other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
    public static Vec3I operator -(Vector3I self, Vector3I other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
    public static Vec3I operator *(Vector3I self, Vec3I other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
    public static Vec3I operator *(Vector3I self, Vector3I other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
    public static Vec3I operator *(Vector3I self, int value) => new(self.X * value, self.Y * value, self.Z * value);
    public static Vec3I operator *(int value, Vector3I self) => new(self.X * value, self.Y * value, self.Z * value);
    public static Vec3I operator /(Vector3I self, Vec3I other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
    public static Vec3I operator /(Vector3I self, Vector3I other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
    public static Vec3I operator /(Vector3I self, int value) => new(self.X / value, self.Y / value, self.Z / value);

    public Vec3I WithX(int x) => new(x, Y, Z);
    public Vec3I WithY(int y) => new(X, y, Z);
    public Vec3I WithZ(int z) => new(X, Y, z);
    public Vec4I To4D(int w) => new(X, Y, Z, w);

    public Vec3I Abs() => new(X.Abs(), Y.Abs(), Z.Abs());
    public int Dot(Vec3I other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
    public int Dot(Vector3I other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);

    private IEnumerable<int> GetEnumerableValues()
    {
        yield return X;
        yield return Y;
        yield return Z;
    }

    public override string ToString() => $"{X}, {Y}, {Z}";
    public override bool Equals(object? obj) => obj is Vector3I v && X == v.X && Y == v.Y && Z == v.Z;
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
}
