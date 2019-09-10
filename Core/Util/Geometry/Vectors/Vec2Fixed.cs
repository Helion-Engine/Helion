using System;
using System.Diagnostics.Contracts;
using System.Numerics;

namespace Helion.Util.Geometry.Vectors
{
    public struct Vec2Fixed
    {
        public Fixed X;
        public Fixed Y;

        public Vec2Fixed(Fixed x, Fixed y)
        {
            X = x;
            Y = y;
        }

        public Vec2Fixed(Vec3Fixed vec)
        {
            X = vec.X;
            Y = vec.Y;
        }

        public static Vec2Fixed operator -(Vec2Fixed self) => new Vec2Fixed(-self.X, -self.Y);
        public static Vec2Fixed operator +(Vec2Fixed self, Vec2Fixed other) => new Vec2Fixed(self.X + other.X, self.Y + other.Y);
        public static Vec2Fixed operator -(Vec2Fixed self, Vec2Fixed other) => new Vec2Fixed(self.X - other.X, self.Y - other.Y);
        public static Vec2Fixed operator *(Vec2Fixed self, Vec2Fixed other) => new Vec2Fixed(self.X * other.X, self.Y * other.Y);
        public static Vec2Fixed operator *(Vec2Fixed self, Fixed value) => new Vec2Fixed(self.X * value, self.Y * value);
        public static Vec2Fixed operator *(Fixed value, Vec2Fixed self) => new Vec2Fixed(self.X * value, self.Y * value);
        public static Vec2Fixed operator *(Vec2Fixed self, int value) => new Vec2Fixed(self.X * value, self.Y * value);
        public static Vec2Fixed operator *(int value, Vec2Fixed self) => new Vec2Fixed(self.X * value, self.Y * value);
        public static Vec2Fixed operator /(Vec2Fixed self, Vec2Fixed other) => new Vec2Fixed(self.X / other.X, self.Y / other.Y);
        public static Vec2Fixed operator /(Vec2Fixed self, Fixed value) => new Vec2Fixed(self.X / value, self.Y / value);
        public static Vec2Fixed operator /(Fixed value, Vec2Fixed self) => new Vec2Fixed(self.X / value, self.Y / value);
        public static Vec2Fixed operator /(Vec2Fixed self, int value) => new Vec2Fixed(self.X / value, self.Y / value);
        public static Vec2Fixed operator <<(Vec2Fixed self, int bits) => new Vec2Fixed(self.X << bits, self.Y << bits);
        public static Vec2Fixed operator >>(Vec2Fixed self, int bits) => new Vec2Fixed(self.X >> bits, self.Y >> bits);
        public static bool operator ==(Vec2Fixed self, Vec2Fixed other) => self.X == other.X && self.Y == other.Y;
        public static bool operator !=(Vec2Fixed self, Vec2Fixed other) => !(self == other);
        
        [Pure]
        public Fixed this[int index] => index == 0 ? X : Y;

        [Pure]
        public Vec2Fixed Abs() => new Vec2Fixed(X.Abs(), Y.Abs());
        
        [Pure]
        public Vec2Fixed Unit() => this / Length();
        
        public void Normalize() => this /= Length();
        
        [Pure]
        public Fixed Dot(Vec2Fixed other) => (X * other.X) + (Y * other.Y);
        
        [Pure]
        public Fixed LengthSquared() => (X * X) + (Y * Y);
        
        [Pure]
        public Fixed Length() => new Fixed(Math.Sqrt(((X * X) + (Y * Y)).ToDouble()));
        
        [Pure]
        public Fixed DistanceSquared(Vec2Fixed other) => (this - other).LengthSquared();
        
        [Pure]
        public Fixed Distance(Vec2Fixed other) => (this - other).Length();
        
        [Pure]
        public Fixed Component(Vec2Fixed onto) => Dot(onto) / onto.Length();
        
        [Pure]
        public Vec2Fixed Projection(Vec2Fixed onto) => Dot(onto) / onto.LengthSquared() * onto;
        
        [Pure]
        public Vec2Fixed Interpolate(Vec2Fixed end, float t) => ToDouble().Interpolate(end.ToDouble(), t).ToFixed();
        
        [Pure]
        public Vec2Fixed OriginRightRotate90() => new Vec2Fixed(Y, -X);
        
        [Pure]
        public Vec2Fixed OriginLeftRotate90() => new Vec2Fixed(-Y, X);
        
        [Pure]
        public Vec2I ToInt() => new Vec2I(X.ToInt(), Y.ToInt());
        
        [Pure]
        public Vector2 ToFloat() => new Vector2(X.ToFloat(), Y.ToFloat());
        
        [Pure]
        public Vec2D ToDouble() => new Vec2D(X.ToDouble(), Y.ToDouble());

        public bool EqualTo(Vec2Fixed other) => Equals(other);
        
        public override string ToString() => $"{X}, {Y}";
        
        public override bool Equals(object? obj) => obj is Vec2Fixed v && X == v.X && Y == v.Y;
        
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }
}