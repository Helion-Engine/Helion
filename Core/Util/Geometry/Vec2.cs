using System;
using System.Numerics;

namespace Helion.Util.Geometry
{
    public struct Vec2i
    {
        public int X;
        public int Y;

        public Vec2i(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Vec2i operator +(Vec2i self, Vec2i other) => new Vec2i(self.X + other.X, self.Y + other.Y);
        public static Vec2i operator -(Vec2i self, Vec2i other) => new Vec2i(self.X - other.X, self.Y - other.Y);
        public static Vec2i operator *(Vec2i self, Vec2i other) => new Vec2i(self.X * other.X, self.Y * other.Y);
        public static Vec2i operator /(Vec2i self, Vec2i other) => new Vec2i(self.X / other.X, self.Y / other.Y);
        public static Vec2i operator <<(Vec2i self, int bits) => new Vec2i(self.X << bits, self.Y << bits);
        public static Vec2i operator >>(Vec2i self, int bits) => new Vec2i(self.X >> bits, self.Y >> bits);
        public static bool operator ==(Vec2i self, Vec2i other) => self.X == other.X && self.Y == other.Y;
        public static bool operator !=(Vec2i self, Vec2i other) => !(self == other);
        public int this[int index] => index == 0 ? X : Y;

        public int LengthSquared() => (X * X) + (Y * Y);
        public int Length() => (int)Math.Sqrt((X * X) + (Y * Y));
        public int DistanceSquared(Vec2i other) => (this - other).LengthSquared();
        public int Distance(Vec2i other) => (this - other).Length();

        public Vec2fixed ToFixed() => new Vec2fixed(new Fixed(X), new Fixed(Y));
        public Vector2 ToFloat() => new Vector2(X, Y);
        public Vec2d ToDouble() => new Vec2d(X, Y);

        public override string ToString() => $"{X}, {Y}";
        public override bool Equals(object obj) => obj is Vec2i v && X == v.X && Y == v.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }

    public struct Vec2d
    {
        public double X;
        public double Y;

        public Vec2d(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Vec2d operator +(Vec2d self, Vec2d other) => new Vec2d(self.X + other.X, self.Y + other.Y);
        public static Vec2d operator -(Vec2d self, Vec2d other) => new Vec2d(self.X - other.X, self.Y - other.Y);
        public static Vec2d operator *(Vec2d self, Vec2d other) => new Vec2d(self.X * other.X, self.Y * other.Y);
        public static Vec2d operator /(Vec2d self, Vec2d other) => new Vec2d(self.X / other.X, self.Y / other.Y);
        public static bool operator ==(Vec2d self, Vec2d other) => self.X == other.X && self.Y == other.Y;
        public static bool operator !=(Vec2d self, Vec2d other) => !(self == other);
        public double this[int index] => index == 0 ? X : Y;

        public double LengthSquared() => (X * X) + (Y * Y);
        public double Length() => Math.Sqrt((X * X) + (Y * Y));
        public double DistanceSquared(Vec2d other) => (this - other).LengthSquared();
        public double Distance(Vec2d other) => (this - other).Length();

        public Vec2fixed ToFixed() => new Vec2fixed(new Fixed(X), new Fixed(Y));
        public Vector2 ToFloat() => new Vector2((float)X, (float)Y);
        public Vec2i ToInt() => new Vec2i((int)X, (int)Y);

        public override string ToString() => $"{X}, {Y}";
        public override bool Equals(object obj) => obj is Vec2i v && X == v.X && Y == v.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }

    public struct Vec2fixed
    {
        public Fixed X;
        public Fixed Y;

        public Vec2fixed(Fixed x, Fixed y)
        {
            X = x;
            Y = y;
        }

        public static Vec2fixed operator +(Vec2fixed self, Vec2fixed other) => new Vec2fixed(self.X + other.X, self.Y + other.Y);
        public static Vec2fixed operator -(Vec2fixed self, Vec2fixed other) => new Vec2fixed(self.X - other.X, self.Y - other.Y);
        public static Vec2fixed operator *(Vec2fixed self, Vec2fixed other) => new Vec2fixed(self.X * other.X, self.Y * other.Y);
        public static Vec2fixed operator /(Vec2fixed self, Vec2fixed other) => new Vec2fixed(self.X / other.X, self.Y / other.Y);
        public static Vec2fixed operator <<(Vec2fixed self, int bits) => new Vec2fixed(self.X << bits, self.Y << bits);
        public static Vec2fixed operator >>(Vec2fixed self, int bits) => new Vec2fixed(self.X >> bits, self.Y >> bits);
        public static bool operator ==(Vec2fixed self, Vec2fixed other) => self.X == other.X && self.Y == other.Y;
        public static bool operator !=(Vec2fixed self, Vec2fixed other) => !(self == other);
        public int this[int index] => index == 0 ? X : Y;

        public int LengthSquared() => (X * X) + (Y * Y);
        public int Length() => new Fixed(Math.Sqrt((X * X) + (Y * Y)));
        public int DistanceSquared(Vec2fixed other) => (this - other).LengthSquared();
        public int Distance(Vec2fixed other) => (this - other).Length();

        public Vec2i ToInt() => new Vec2i(X, Y);
        public Vector2 ToFloat() => new Vector2(X, Y);
        public Vec2d ToDouble() => new Vec2d(X, Y);

        public override string ToString() => $"{X}, {Y}";
        public override bool Equals(object obj) => obj is Vec2i v && X == v.X && Y == v.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }

    public static class Vector2Extensions
    {
        public static float LengthSquared(this Vector2 vec) => (vec.X * vec.X) + (vec.Y * vec.Y);
        public static float Length(this Vector2 vec) => (float)Math.Sqrt(LengthSquared(vec));
        public static float DistanceSquared(this Vector2 vec, Vector2 other) => (vec - other).LengthSquared();
        public static float Distance(this Vector2 vec, Vector2 other) => (vec - other).Length();

        public static Vec2i ToInt(this Vector2 vec) => new Vec2i((int)vec.X, (int)vec.Y);
        public static Vec2fixed ToFixed(this Vector2 vec) => new Vec2fixed(new Fixed(vec.X), new Fixed(vec.Y));
        public static Vec2d ToDouble(this Vector2 vec) => new Vec2d(vec.X, vec.Y);
    }
}
