using System;
using System.Diagnostics.Contracts;
using System.Numerics;

namespace Helion.Util.Geometry.Vectors
{
    public struct Vec2I
    {
        public static readonly Vec2I Zero = new(0, 0);

        public int X;
        public int Y;

        public bool IsOrigin => this == Zero;

        public Vec2I(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vec2I(Vec3I vec)
        {
            X = vec.X;
            Y = vec.Y;
        }

        public void Add(int x, int y)
        {
            X += x;
            Y += y;
        }

        public static Vec2I operator -(Vec2I self) => new Vec2I(-self.X, -self.Y);
        public static Vec2I operator +(Vec2I self, Vec2I other) => new Vec2I(self.X + other.X, self.Y + other.Y);
        public static Vec2I operator -(Vec2I self, Vec2I other) => new Vec2I(self.X - other.X, self.Y - other.Y);
        public static Vec2I operator *(Vec2I self, Vec2I other) => new Vec2I(self.X * other.X, self.Y * other.Y);
        public static Vec2I operator *(Vec2I self, int value) => new Vec2I(self.X * value, self.Y * value);
        public static Vec2I operator /(Vec2I self, Vec2I other) => new Vec2I(self.X / other.X, self.Y / other.Y);
        public static Vec2I operator /(Vec2I self, int value) => new Vec2I(self.X / value, self.Y / value);
        public static Vec2I operator <<(Vec2I self, int bits) => new Vec2I(self.X << bits, self.Y << bits);
        public static Vec2I operator >>(Vec2I self, int bits) => new Vec2I(self.X >> bits, self.Y >> bits);
        public static bool operator ==(Vec2I self, Vec2I other) => self.X == other.X && self.Y == other.Y;
        public static bool operator !=(Vec2I self, Vec2I other) => !(self == other);
        public int this[int index] => index == 0 ? X : Y;

        [Pure]
        public Vec2I Abs() => new Vec2I(Math.Abs(X), Math.Abs(Y));

        [Pure]
        public int Dot(Vec2I other) => (X * other.X) + (Y * other.Y);

        [Pure]
        public int LengthSquared() => (X * X) + (Y * Y);

        [Pure]
        public int Length() => (int)Math.Sqrt((X * X) + (Y * Y));

        [Pure]
        public int DistanceSquared(Vec2I other) => (this - other).LengthSquared();

        [Pure]
        public int Distance(Vec2I other) => (this - other).Length();

        [Pure]
        public Vec2I OriginRightRotate90() => new Vec2I(Y, -X);

        [Pure]
        public Vec2I OriginLeftRotate90() => new Vec2I(-Y, X);

        [Pure]
        public Vec2Fixed ToFixed() => new Vec2Fixed(new Fixed(X), new Fixed(Y));

        [Pure]
        public Vector2 ToFloat() => new Vector2(X, Y);

        [Pure]
        public Vec2D ToDouble() => new Vec2D(X, Y);

        public override string ToString() => $"{X}, {Y}";

        public override bool Equals(object? obj) => obj is Vec2I v && X == v.X && Y == v.Y;

        public override int GetHashCode() => HashCode.Combine(X, Y);
    }
}