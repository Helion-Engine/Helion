using System;
using System.Numerics;

namespace Helion.Util.Geometry
{
    public struct Vec2I
    {
        public int X;
        public int Y;

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

        public static Vec2I operator +(Vec2I self, Vec2I other) => new Vec2I(self.X + other.X, self.Y + other.Y);
        public static Vec2I operator -(Vec2I self, Vec2I other) => new Vec2I(self.X - other.X, self.Y - other.Y);
        public static Vec2I operator *(Vec2I self, Vec2I other) => new Vec2I(self.X * other.X, self.Y * other.Y);
        public static Vec2I operator /(Vec2I self, Vec2I other) => new Vec2I(self.X / other.X, self.Y / other.Y);
        public static Vec2I operator <<(Vec2I self, int bits) => new Vec2I(self.X << bits, self.Y << bits);
        public static Vec2I operator >>(Vec2I self, int bits) => new Vec2I(self.X >> bits, self.Y >> bits);
        public static bool operator ==(Vec2I self, Vec2I other) => self.X == other.X && self.Y == other.Y;
        public static bool operator !=(Vec2I self, Vec2I other) => !(self == other);
        public int this[int index] => index == 0 ? X : Y;

        public Vec2I Abs() => new Vec2I(Math.Abs(X), Math.Abs(Y));
        public int Dot(Vec2I other) => (X * other.X) + (Y * other.Y);
        public int LengthSquared() => (X * X) + (Y * Y);
        public int Length() => (int)Math.Sqrt((X * X) + (Y * Y));
        public int DistanceSquared(Vec2I other) => (this - other).LengthSquared();
        public int Distance(Vec2I other) => (this - other).Length();

        public Vec2Fixed ToFixed() => new Vec2Fixed(new Fixed(X), new Fixed(Y));
        public Vector2 ToFloat() => new Vector2(X, Y);
        public Vec2D ToDouble() => new Vec2D(X, Y);

        public override string ToString() => $"{X}, {Y}";
        public override bool Equals(object obj) => obj is Vec2I v && X == v.X && Y == v.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }

    public struct Vec2D
    {
        public double X;
        public double Y;

        public Vec2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Vec2D(Vec3D vec)
        {
            X = vec.X;
            Y = vec.Y;
        }

        public static Vec2D operator +(Vec2D self, Vec2D other) => new Vec2D(self.X + other.X, self.Y + other.Y);
        public static Vec2D operator -(Vec2D self, Vec2D other) => new Vec2D(self.X - other.X, self.Y - other.Y);
        public static Vec2D operator *(Vec2D self, Vec2D other) => new Vec2D(self.X * other.X, self.Y * other.Y);
        public static Vec2D operator *(Vec2D self, double value) => new Vec2D(self.X * value, self.Y * value);
        public static Vec2D operator *(double value, Vec2D self) => new Vec2D(self.X * value, self.Y * value);
        public static Vec2D operator /(Vec2D self, Vec2D other) => new Vec2D(self.X / other.X, self.Y / other.Y);
        public static Vec2D operator /(Vec2D self, double value) => new Vec2D(self.X / value, self.Y / value);
        public static Vec2D operator /(double value, Vec2D self) => new Vec2D(self.X / value, self.Y / value);
        public static bool operator ==(Vec2D self, Vec2D other) => self.X == other.X && self.Y == other.Y;
        public static bool operator !=(Vec2D self, Vec2D other) => !(self == other);
        public double this[int index] => index == 0 ? X : Y;

        public Vec2D Abs() => new Vec2D(Math.Abs(X), Math.Abs(Y));
        public Vec2D Unit() => this / Length();
        public void Normalize() => this /= Length();
        public double Dot(Vec2D other) => (X * other.X) + (Y * other.Y);
        public double LengthSquared() => (X * X) + (Y * Y);
        public double Length() => Math.Sqrt((X * X) + (Y * Y));
        public double DistanceSquared(Vec2D other) => (this - other).LengthSquared();
        public double Distance(Vec2D other) => (this - other).Length();

        public Vec2Fixed ToFixed() => new Vec2Fixed(new Fixed(X), new Fixed(Y));
        public Vector2 ToFloat() => new Vector2((float)X, (float)Y);
        public Vec2I ToInt() => new Vec2I((int)X, (int)Y);

        public bool EqualTo(Vec2D other, double epsilon = 0.00001)
        {
            return MathHelper.AreEqual(X, other.X, epsilon) && MathHelper.AreEqual(Y, other.Y, epsilon);
        }

        public override string ToString() => $"{X}, {Y}";
        public override bool Equals(object obj) => obj is Vec2I v && X == v.X && Y == v.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }

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
        public Fixed this[int index] => index == 0 ? X : Y;

        public Vec2Fixed Abs() => new Vec2Fixed(X.Abs(), Y.Abs());
        public Vec2Fixed Unit() => this / Length();
        public void Normalize() => this /= Length();
        public Fixed Dot(Vec2Fixed other) => (X * other.X) + (Y * other.Y);
        public Fixed LengthSquared() => (X * X) + (Y * Y);
        public Fixed Length() => new Fixed(Math.Sqrt(((X * X) + (Y * Y)).ToDouble()));
        public Fixed DistanceSquared(Vec2Fixed other) => (this - other).LengthSquared();
        public Fixed Distance(Vec2Fixed other) => (this - other).Length();

        public Vec2I ToInt() => new Vec2I(X.ToInt(), Y.ToInt());
        public Vector2 ToFloat() => new Vector2(X.ToFloat(), Y.ToFloat());
        public Vec2D ToDouble() => new Vec2D(X.ToDouble(), Y.ToDouble());

        public bool EqualTo(Vec2Fixed other) => Equals(other);
        public override string ToString() => $"{X}, {Y}";
        public override bool Equals(object obj) => obj is Vec2Fixed v && X == v.X && Y == v.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }

    public static class Vector2Extensions
    {
        public static Vector2 Abs(this Vector2 vec) => new Vector2(Math.Abs(vec.X), Math.Abs(vec.Y));
        public static float Dot(this Vector2 vec, Vector2 other) => (vec.X * other.X) + (vec.Y * other.Y);
        public static Vector2 Unit(this Vector2 vec) => vec / vec.Length();
        public static float LengthSquared(this Vector2 vec) => (vec.X * vec.X) + (vec.Y * vec.Y);
        public static float Length(this Vector2 vec) => (float)Math.Sqrt((vec.X * vec.X) + (vec.Y * vec.Y));
        public static float DistanceSquared(this Vector2 vec, Vector2 other) => (vec - other).LengthSquared();
        public static float Distance(this Vector2 vec, Vector2 other) => (vec - other).Length();
        public static Vec2I ToInt(this Vector2 vec) => new Vec2I((int)vec.X, (int)vec.Y);
        public static Vec2Fixed ToFixed(this Vector2 vec) => new Vec2Fixed(new Fixed(vec.X), new Fixed(vec.Y));
        public static Vec2D ToDouble(this Vector2 vec) => new Vec2D(vec.X, vec.Y);

        public static bool EqualTo(this Vector2 vec, Vector2 other, float epsilon = 0.00001f)
        {
            return MathHelper.AreEqual(vec.X, other.X, epsilon) && MathHelper.AreEqual(vec.Y, other.Y, epsilon);
        }

        public static void Deconstruct(this Vector2 vec, out float x, out float y)
        {
            x = vec.X;
            y = vec.Y;
        }
    }
}
