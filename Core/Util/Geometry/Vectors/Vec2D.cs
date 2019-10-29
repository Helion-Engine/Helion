using System;
using System.Diagnostics.Contracts;
using System.Numerics;

namespace Helion.Util.Geometry.Vectors
{
    public struct Vec2D
    {
        public static readonly Vec2D Zero = new Vec2D(0, 0);
        
        public double X;
        public double Y;

        public double U => X;
        public double V => Y;

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

        /// <summary>
        /// Takes some radian value and calculates the unit circle vector.
        /// </summary>
        /// <param name="radians">The radian angle.</param>
        /// <returns>A unit vector to a point on a unit circle based on the
        /// provided angle.</returns>
        public static Vec2D RadiansToUnit(double radians) => new Vec2D(Math.Cos(radians), Math.Sin(radians));

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

        public Vec3D To3D(double z) => new Vec3D(X, Y, z);
    }
}