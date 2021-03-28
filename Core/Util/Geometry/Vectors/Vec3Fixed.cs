using System;
using System.Numerics;
using Helion.Geometry;

namespace Helion.Util.Geometry.Vectors
{
    public struct Vec3Fixed
    {
        public Fixed X;
        public Fixed Y;
        public Fixed Z;

        public Vec3Fixed(Fixed x, Fixed y, Fixed z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3Fixed operator +(Vec3Fixed self, Vec3Fixed other) => new Vec3Fixed(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3Fixed operator -(Vec3Fixed self, Vec3Fixed other) => new Vec3Fixed(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3Fixed operator *(Vec3Fixed self, Vec3Fixed other) => new Vec3Fixed(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3Fixed operator *(Vec3Fixed self, Fixed value) => new Vec3Fixed(self.X * value, self.Y * value, self.Z * value);
        public static Vec3Fixed operator *(Fixed value, Vec3Fixed self) => new Vec3Fixed(self.X * value, self.Y * value, self.Z * value);
        public static Vec3Fixed operator /(Vec3Fixed self, Vec3Fixed other) => new Vec3Fixed(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3Fixed operator /(Vec3Fixed self, Fixed value) => new Vec3Fixed(self.X / value, self.Y / value, self.Z / value);
        public static Vec3Fixed operator /(Fixed value, Vec3Fixed self) => new Vec3Fixed(self.X / value, self.Y / value, self.Z / value);
        public static bool operator !=(Vec3Fixed self, Vec3Fixed other) => !(self == other);
        public static bool operator ==(Vec3Fixed self, Vec3Fixed other)
        {
            return MathHelper.AreEqual(self.X, other.X) && MathHelper.AreEqual(self.Y, other.Y) && MathHelper.AreEqual(self.Z, other.Z);
        }
        
        public Fixed this[int index] 
        {
            get 
            {
                switch (index)
                {
                default:
                    return X;
                case 1:
                    return Y;
                case 2:
                    return Z;
                }
            }
        }

        public Vec3Fixed Abs() => new Vec3Fixed(X.Abs(), Y.Abs(), Z.Abs());
        public Vec3Fixed Unit() => this / Length();
        public void Normalize() => this /= Length();
        public Fixed Dot(Vec3Fixed other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public Fixed LengthSquared() => (X * X) + (Y * Y) + (Z * Z);
        public Fixed Length() => new Fixed(Math.Sqrt(((X * X) + (Y * Y) + (Z * Z)).ToDouble()));
        public Fixed DistanceSquared(Vec3Fixed other) => (this - other).LengthSquared();
        public Fixed Distance(Vec3Fixed other) => (this - other).Length();
        public Vec3Fixed Interpolate(Vec3Fixed end, float t) => ToDouble().Interpolate(end.ToDouble(), t).ToFixed();

        public Vec2Fixed To2D() => new Vec2Fixed(X, Y);
        public Vec3D ToDouble() => new Vec3D(X.ToDouble(), Y.ToDouble(), Z.ToDouble());
        public Vector3 ToFloat() => new Vector3(X.ToFloat(), Y.ToFloat(), Z.ToFloat());
        public Vec3I ToInt() => new Vec3I(X.ToInt(), Y.ToInt(), Z.ToInt());

        public override string ToString() => $"{X}, {Y}, {Z}";
        public override bool Equals(object? obj) => obj is Vec3Fixed v && X == v.X && Y == v.Y && Z == v.Z;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }
}