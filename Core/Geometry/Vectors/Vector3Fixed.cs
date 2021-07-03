// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using GlmSharp;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Util.Extensions;

namespace Helion.Geometry.Vectors
{
    public class Vector3Fixed
    {
        public static readonly Vector3Fixed Zero = new(Fixed.Zero(), Fixed.Zero(), Fixed.Zero());
        public static readonly Vector3Fixed One = new(Fixed.One(), Fixed.One(), Fixed.One());

        public Fixed X;
        public Fixed Y;
        public Fixed Z;

        public Fixed U => X;
        public Fixed V => Y;
        public Vec2Fixed XY => new(X, Y);
        public Vec2Fixed XZ => new(X, Z);
        public Vec3I Int => new(X.ToInt(), Y.ToInt(), Z.ToInt());
        public Vec3F Float => new(X.ToFloat(), Y.ToFloat(), Z.ToFloat());
        public Vec3D Double => new(X.ToDouble(), Y.ToDouble(), Z.ToDouble());
        public Vec3Fixed Struct => new(X, Y, Z);
        public IEnumerable<Fixed> Values => GetEnumerableValues();

        public Vector3Fixed(Fixed x, Fixed y, Fixed z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Deconstruct(out Fixed x, out Fixed y, out Fixed z)
        {
            x = X;
            y = Y;
            z = Z;
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

        public static Vec3Fixed operator -(Vector3Fixed self) => new(-self.X, -self.Y, -self.Z);
        public static Vec3Fixed operator +(Vector3Fixed self, Vec3Fixed other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3Fixed operator +(Vector3Fixed self, Vector3Fixed other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3Fixed operator -(Vector3Fixed self, Vec3Fixed other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3Fixed operator -(Vector3Fixed self, Vector3Fixed other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3Fixed operator *(Vector3Fixed self, Vec3Fixed other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3Fixed operator *(Vector3Fixed self, Vector3Fixed other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3Fixed operator *(Vector3Fixed self, Fixed value) => new(self.X * value, self.Y * value, self.Z * value);
        public static Vec3Fixed operator *(Fixed value, Vector3Fixed self) => new(self.X * value, self.Y * value, self.Z * value);
        public static Vec3Fixed operator /(Vector3Fixed self, Vec3Fixed other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3Fixed operator /(Vector3Fixed self, Vector3Fixed other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3Fixed operator /(Vector3Fixed self, Fixed value) => new(self.X / value, self.Y / value, self.Z / value);

        public Vec3Fixed WithX(Fixed x) => new(x, Y, Z);
        public Vec3Fixed WithY(Fixed y) => new(X, y, Z);
        public Vec3Fixed WithZ(Fixed z) => new(X, Y, z);
        public Vec4Fixed To4D(Fixed w) => new(X, Y, Z, w);

        public Vec3Fixed Abs() => new(X.Abs(), Y.Abs(), Z.Abs());
        public Fixed Dot(Vec3Fixed other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public Fixed Dot(Vector3Fixed other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);

        private IEnumerable<Fixed> GetEnumerableValues()
        {
            yield return X;
            yield return Y;
            yield return Z;
        }

        public override string ToString() => $"{X}, {Y}, {Z}";
        public override bool Equals(object? obj) => obj is Vector3Fixed v && X == v.X && Y == v.Y && Z == v.Z;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }
}
