// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Util.Extensions;

namespace Helion.Geometry.Vectors
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct Vec2D(double x, double y) : IEquatable<Vec2D>
    {
        public static readonly Vec2D Zero = new(0, 0);
        public static readonly Vec2D One = new(1, 1);

        public double X = x;
        public double Y = y;

        public readonly Vec2I Int => new((int)X, (int)Y);
        public readonly Vec2F Float => new((float)X, (float)Y);
        public readonly Vec2Fixed FixedPoint => new(Fixed.From(X), Fixed.From(Y));
        public readonly Box2D Box => new((0, 0), (X, Y));

        public static implicit operator Vec2D(ValueTuple<double, double> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public static Vec2D operator -(Vec2D self) => new(-self.X, -self.Y);
        public static Vec2D operator +(Vec2D self, Vec2D other) => new(self.X + other.X, self.Y + other.Y);
        public static Vec2D operator +(Vec2D self, Vector2D other) => new(self.X + other.X, self.Y + other.Y);
        public static Vec2D operator -(Vec2D self, Vec2D other) => new(self.X - other.X, self.Y - other.Y);
        public static Vec2D operator -(Vec2D self, Vector2D other) => new(self.X - other.X, self.Y - other.Y);
        public static Vec2D operator *(Vec2D self, Vec2D other) => new(self.X * other.X, self.Y * other.Y);
        public static Vec2D operator *(Vec2D self, Vector2D other) => new(self.X * other.X, self.Y * other.Y);
        public static Vec2D operator *(Vec2D self, double value) => new(self.X * value, self.Y * value);
        public static Vec2D operator *(double value, Vec2D self) => new(self.X * value, self.Y * value);
        public static Vec2D operator /(Vec2D self, Vec2D other) => new(self.X / other.X, self.Y / other.Y);
        public static Vec2D operator /(Vec2D self, Vector2D other) => new(self.X / other.X, self.Y / other.Y);
        public static Vec2D operator /(Vec2D self, double value) => new(self.X / value, self.Y / value);
        public static bool operator ==(Vec2D self, Vec2D other) => self.X == other.X && self.Y == other.Y;
        public static bool operator !=(Vec2D self, Vec2D other) => !(self == other);

        public readonly Vec2D WithX(double x) => new(x, Y);
        public readonly Vec2D WithY(double y) => new(X, y);
        public readonly bool IsApprox(Vec2D other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y);
        public readonly bool IsApprox(Vector2D other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y);
        public readonly Vec3D To3D(double z) => new(X, Y, z);

        public readonly Vec2D Abs() => new(X.Abs(), Y.Abs());
        public readonly Vec2D Floor() => new(X.Floor(), Y.Floor());
        public readonly Vec2D Ceiling() => new(X.Ceiling(), Y.Ceiling());
        public readonly Vec2D Unit() => this / Length();
        public void Normalize() => this /= Length();
        public readonly double LengthSquared() => (X * X) + (Y * Y);
        public readonly Vec2D Inverse() => new(1 / X, 1 / Y);
        public readonly Rotation Rotation(Vec2D second, Vec2D third, double epsilon = 0.000001) => new Seg2D(this, second).ToSide(third, epsilon);
        public readonly Rotation Rotation(Vector2D second, Vector2D third, double epsilon = 0.000001) => new Seg2D(this, second).ToSide(third, epsilon);
        public readonly double Length() => Math.Sqrt(LengthSquared());
        public readonly double DistanceSquared(Vec2D other) => (this - other).LengthSquared();
        public readonly double DistanceSquared(Vector2D other) => (this - other).LengthSquared();
        public readonly double Distance(Vec2D other) => (this - other).Length();
        public readonly double Distance(Vector2D other) => (this - other).Length();
        public readonly Vec2D Interpolate(Vec2D end, double t) => this + (t * (end - this));
        public readonly Vec2D Interpolate(Vector2D end, double t) => this + (t * (end - this));
        public readonly double Dot(Vec2D other) => (X * other.X) + (Y * other.Y);
        public readonly double Dot(Vector2D other) => (X * other.X) + (Y * other.Y);
        public readonly double Component(Vec2D onto) => Dot(onto) / onto.Length();
        public readonly double Component(Vector2D onto) => Dot(onto) / onto.Length();
        public readonly Vec2D Projection(Vec2D onto) => Dot(onto) / onto.LengthSquared() * onto;
        public readonly Vec2D Projection(Vector2D onto) => Dot(onto) / onto.LengthSquared() * onto;
        public readonly Vec2D RotateRight90() => new(Y, -X);
        public readonly Vec2D RotateLeft90() => new(-Y, X);
        public readonly Vec2D Rotate(double radians)
        {
            double sin = Math.Sin(radians);
            double cos = Math.Cos(radians);
            return new((X * cos) - (Y * sin), (X * sin) + (Y * cos));
        }
        public static Vec2D Rotate(double x, double y, double radians)
        {
            double sin = Math.Sin(radians);
            double cos = Math.Cos(radians);
            return new((x * cos) - (y * sin), (x * sin) + (y * cos));
        }
        public static Vec2D UnitCircle(double radians) => new(Math.Cos(radians), Math.Sin(radians));
        public readonly double Angle(in Vec2D other) => Math.Atan2(other.Y - Y, other.X - X);
        public readonly double Angle(Vector2D other) => Math.Atan2(other.Y - Y, other.X - X);
        public readonly double Angle(in Vec3D other) => Math.Atan2(other.Y - Y, other.X - X);
        public readonly double Angle(Vector3D other) => Math.Atan2(other.Y - Y, other.X - X);

        public override readonly string ToString() => $"{X}, {Y}";
        public override readonly int GetHashCode() => HashCode.Combine(X, Y);
        public readonly bool Equals(Vec2D other) => X == other.X && Y == other.Y;
        public readonly override bool Equals(object? obj) => obj is not null && obj is Vec2D v && Equals(v);
    }
}
