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

namespace Helion.Geometry.Vectors
{
    public class Vector4Fixed
    {
        public static readonly Vector4Fixed Zero = new(Fixed.Zero(), Fixed.Zero(), Fixed.Zero(), Fixed.Zero());
        public static readonly Vector4Fixed One = new(Fixed.One(), Fixed.One(), Fixed.One(), Fixed.One());

        public Fixed X;
        public Fixed Y;
        public Fixed Z;
        public Fixed W;

        public Fixed U => X;
        public Fixed V => Y;
        public Vec2Fixed XY => new(X, Y);
        public Vec2Fixed XZ => new(X, Z);
        public Vec3Fixed XYZ => new(X, Y, Z);
        public Vec4I Int => new(X.ToInt(), Y.ToInt(), Z.ToInt(), W.ToInt());
        public Vec4F Float => new(X.ToFloat(), Y.ToFloat(), Z.ToFloat(), W.ToFloat());
        public Vec4D Double => new(X.ToDouble(), Y.ToDouble(), Z.ToDouble(), W.ToDouble());
        public Vec4Fixed Struct => new(X, Y, Z, W);
        public IEnumerable<Fixed> Values => GetEnumerableValues();

        public Vector4Fixed(Fixed x, Fixed y, Fixed z, Fixed w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public void Deconstruct(out Fixed x, out Fixed y, out Fixed z, out Fixed w)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        public Fixed this[int index]
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

        public static Vec4Fixed operator -(Vector4Fixed self) => new(-self.X, -self.Y, -self.Z, -self.W);
        public static Vec4Fixed operator +(Vector4Fixed self, Vec4Fixed other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z, self.W + other.W);
        public static Vec4Fixed operator +(Vector4Fixed self, Vector4Fixed other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z, self.W + other.W);
        public static Vec4Fixed operator -(Vector4Fixed self, Vec4Fixed other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z, self.W - other.W);
        public static Vec4Fixed operator -(Vector4Fixed self, Vector4Fixed other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z, self.W - other.W);
        public static Vec4Fixed operator *(Vector4Fixed self, Vec4Fixed other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z, self.W * other.W);
        public static Vec4Fixed operator *(Vector4Fixed self, Vector4Fixed other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z, self.W * other.W);
        public static Vec4Fixed operator *(Vector4Fixed self, Fixed value) => new(self.X * value, self.Y * value, self.Z * value, self.W * value);
        public static Vec4Fixed operator *(Fixed value, Vector4Fixed self) => new(self.X * value, self.Y * value, self.Z * value, self.W * value);
        public static Vec4Fixed operator /(Vector4Fixed self, Vec4Fixed other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z, self.W / other.W);
        public static Vec4Fixed operator /(Vector4Fixed self, Vector4Fixed other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z, self.W / other.W);
        public static Vec4Fixed operator /(Vector4Fixed self, Fixed value) => new(self.X / value, self.Y / value, self.Z / value, self.W / value);

        public Vec4Fixed WithX(Fixed x) => new(x, Y, Z, W);
        public Vec4Fixed WithY(Fixed y) => new(X, y, Z, W);
        public Vec4Fixed WithZ(Fixed z) => new(X, Y, z, W);
        public Vec4Fixed WithW(Fixed w) => new(X, Y, Z, w);

        public Vec4Fixed Abs() => new(X.Abs(), Y.Abs(), Z.Abs(), W.Abs());
        public Fixed Dot(Vec4Fixed other) => (X * other.X) + (Y * other.Y) + (Z * other.Z) + (W * other.W);
        public Fixed Dot(Vector4Fixed other) => (X * other.X) + (Y * other.Y) + (Z * other.Z) + (W * other.W);

        private IEnumerable<Fixed> GetEnumerableValues()
        {
            yield return X;
            yield return Y;
            yield return Z;
            yield return W;
        }

        public override string ToString() => $"{X}, {Y}, {Z}, {W}";
        public override bool Equals(object? obj) => obj is Vector4Fixed v && X == v.X && Y == v.Y && Z == v.Z && W == v.W;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    }
}
