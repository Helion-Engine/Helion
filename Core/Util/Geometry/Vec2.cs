using System;
using System.Diagnostics.Contracts;
using System.Numerics;

namespace Helion.Util.Geometry
{
    public struct Vec2I
    {
        public static readonly Vec2I Zero = new Vec2I(0, 0);
        
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
        public static Vec2I operator /(Vec2I self, Vec2I other) => new Vec2I(self.X / other.X, self.Y / other.Y);
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

    public struct Vec2D
    {
        public static readonly Vec2D Zero = new Vec2D(0, 0);
        
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

        public static Vec2D operator -(Vec2D self) => new Vec2D(-self.X, -self.Y);
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
        
        [Pure]
        public double this[int index] => index == 0 ? X : Y;

        [Pure]
        public Vec2D Floor() => new Vec2D(Math.Floor(X), Math.Floor(Y));
        
        [Pure]
        public Vec2D Ceil() => new Vec2D(Math.Ceiling(X), Math.Ceiling(Y));
        
        [Pure]
        public Vec2D Abs() => new Vec2D(Math.Abs(X), Math.Abs(Y));
        
        [Pure]
        public Vec2D Unit() => this / Length();
        
        public void Normalize() => this /= Length();
        
        [Pure]
        public double Dot(Vec2D other) => (X * other.X) + (Y * other.Y);
        
        [Pure]
        public double LengthSquared() => (X * X) + (Y * Y);
        
        [Pure]
        public double Length() => Math.Sqrt((X * X) + (Y * Y));
        
        [Pure]
        public double Component(Vec2D onto) => Dot(onto) / onto.Length();
        
        [Pure]
        public Vec2D Projection(Vec2D onto) => Dot(onto) / onto.LengthSquared() * onto;
        
        [Pure]
        public double DistanceSquared(Vec2D other) => (this - other).LengthSquared();
        
        [Pure]
        public double Distance(Vec2D other) => (this - other).Length();
        
        [Pure]
        public Vec2D Interpolate(Vec2D end, double t) => this + (t * (end - this));
        
        [Pure]
        public Vec2D OriginRightRotate90() => new Vec2D(Y, -X);
        
        [Pure]
        public Vec2D OriginLeftRotate90() => new Vec2D(-Y, X);

        [Pure]
        public Vec2Fixed ToFixed() => new Vec2Fixed(new Fixed(X), new Fixed(Y));
        
        [Pure]
        public Vector2 ToFloat() => new Vector2((float)X, (float)Y);
        
        [Pure]
        public Vec2I ToInt() => new Vec2I((int)X, (int)Y);
        
        public bool EqualTo(Vec2D other, double epsilon = 0.00001)
        {
            return MathHelper.AreEqual(X, other.X, epsilon) && MathHelper.AreEqual(Y, other.Y, epsilon);
        }

        public override string ToString() => $"{X}, {Y}";
        
        public override bool Equals(object? obj) => obj is Vec2I v && X == v.X && Y == v.Y;
        
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

    public static class Vector2Extensions
    {
        public static float U(this Vector2 vec) => vec.X;
        public static float V(this Vector2 vec) => vec.Y;
        public static Vector2 Abs(this Vector2 vec) => new Vector2(Math.Abs(vec.X), Math.Abs(vec.Y));
        public static float Dot(this Vector2 vec, Vector2 other) => (vec.X * other.X) + (vec.Y * other.Y);
        public static Vector2 Unit(this Vector2 vec) => vec / vec.Length();
        public static float LengthSquared(this Vector2 vec) => (vec.X * vec.X) + (vec.Y * vec.Y);
        public static float Length(this Vector2 vec) => (float)Math.Sqrt((vec.X * vec.X) + (vec.Y * vec.Y));
        public static float DistanceSquared(this Vector2 vec, Vector2 other) => (vec - other).LengthSquared();
        public static float Distance(this Vector2 vec, Vector2 other) => (vec - other).Length();
        public static float Component(this Vector2 vec, Vector2 onto) => vec.Dot(onto) / onto.Length();
        public static Vector2 Projection(this Vector2 vec, Vector2 onto) => vec.Dot(onto) / onto.LengthSquared() * onto;
        public static Vector2 Interpolate(this Vector2 start, Vector2 end, float t) => start + (t * (end - start));
        public static Vec2I ToInt(this Vector2 vec) => new Vec2I((int)vec.X, (int)vec.Y);
        public static Vec2Fixed ToFixed(this Vector2 vec) => new Vec2Fixed(new Fixed(vec.X), new Fixed(vec.Y));
        public static Vec2D ToDouble(this Vector2 vec) => new Vec2D(vec.X, vec.Y);
        public static Vector2 OriginRightRotate90(this Vector2 vec) => new Vector2(vec.Y, -vec.X);
        public static Vector2 OriginLeftRotate90(this Vector2 vec) => new Vector2(-vec.Y, vec.X);

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