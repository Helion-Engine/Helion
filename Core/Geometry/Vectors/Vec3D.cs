// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using Helion.Geometry.Boxes;
using Helion.Util.Extensions;

namespace Helion.Geometry.Vectors
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct Vec3D(double x, double y, double z) : IEquatable<Vec3D>
    {
        public static readonly Vec3D Zero = new(0, 0, 0);
        public static readonly Vec3D One = new(1, 1, 1);

        public double X = x;
        public double Y = y;
        public double Z = z;

        public readonly Vec2D XY => new(X, Y);
        public readonly Vec2D XZ => new(X, Z);
        public readonly Vec3I Int => new((int)X, (int)Y, (int)Z);
        public readonly Vec3F Float => new((float)X, (float)Y, (float)Z);
        public readonly Vec3Fixed FixedPoint => new(Fixed.From(X), Fixed.From(Y), Fixed.From(Z));
        public readonly Box3D Box => new((0, 0, 0), (X, Y, Z));

        public static implicit operator Vec3D(ValueTuple<double, double, double> tuple)
        {
            return new(tuple.Item1, tuple.Item2, tuple.Item3);
        }

        public static Vec3D operator -(Vec3D self) => new(-self.X, -self.Y, -self.Z);
        public static Vec3D operator +(Vec3D self, Vec3D other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3D operator +(Vec3D self, Vector3D other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3D operator -(Vec3D self, Vec3D other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3D operator -(Vec3D self, Vector3D other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3D operator *(Vec3D self, Vec3D other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3D operator *(Vec3D self, Vector3D other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3D operator *(Vec3D self, double value) => new(self.X * value, self.Y * value, self.Z * value);
        public static Vec3D operator *(double value, Vec3D self) => new(self.X * value, self.Y * value, self.Z * value);
        public static Vec3D operator /(Vec3D self, Vec3D other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3D operator /(Vec3D self, Vector3D other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3D operator /(Vec3D self, double value) => new(self.X / value, self.Y / value, self.Z / value);
        public static bool operator ==(Vec3D self, Vec3D other) => self.X == other.X && self.Y == other.Y && self.Z == other.Z;
        public static bool operator !=(Vec3D self, Vec3D other) => !(self == other);

        public readonly Vec3D WithX(double x) => new(x, Y, Z);
        public readonly Vec3D WithY(double y) => new(X, y, Z);
        public readonly Vec3D WithZ(double z) => new(X, Y, z);
        public readonly bool IsApprox(Vec3D other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y) && Z.ApproxEquals(other.Z);
        public readonly bool IsApprox(Vector3D other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y) && Z.ApproxEquals(other.Z);
        public readonly Vec4D To4D(double w) => new(X, Y, Z, w);

        public readonly Vec3D Abs() => new(X.Abs(), Y.Abs(), Z.Abs());
        public readonly Vec3D Floor() => new(X.Floor(), Y.Floor(), Z.Floor());
        public readonly Vec3D Ceiling() => new(X.Ceiling(), Y.Ceiling(), Z.Ceiling());
        public readonly Vec3D Unit() => this / Length();
        public void Normalize() => this /= Length();
        public readonly double LengthSquared() => (X * X) + (Y * Y) + (Z * Z);
        public readonly Vec3D Inverse() => new(1 / X, 1 / Y, 1 / Z);
        public readonly double Length() => Math.Sqrt(LengthSquared());
        public readonly double DistanceSquared(Vec3D other) => (this - other).LengthSquared();
        public readonly double Distance(Vec3D other) => (this - other).Length();
        public readonly Vec3D Interpolate(Vec3D end, double t) => this + (t * (end - this));
        public readonly double Dot(Vec3D other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public static Vec3D UnitSphere(double angle, double pitch)
        {
            double sinAngle = Math.Sin(angle);
            double cosAngle = Math.Cos(angle);
            double sinPitch = Math.Sin(pitch);
            double cosPitch = Math.Cos(pitch);
            return new(cosAngle * cosPitch, sinAngle * cosPitch, sinPitch);
        }
        public readonly Vec3D Rotate2D(double yawRadians)
        {
            double sin = Math.Sin(yawRadians);
            double cos = Math.Cos(yawRadians);
            return new((X * cos) - (Y * sin), (X * sin) + (Y * cos), Z);
        }
        public readonly double Pitch(in Vec3D other, double length) => Math.Atan2(other.Z - Z, length);
        public readonly double Pitch(double z, double length) => Math.Atan2(z - Z, length);
        public readonly double Angle(in Vec3D other) => Math.Atan2(other.Y - Y, other.X - X);
        public readonly double Angle(in Vec2D other) => Math.Atan2(other.Y - Y, other.X - X);
        public readonly double ApproximateDistance2D(in Vec3D other)
        {
            double dx = Math.Abs(X - other.X);
            double dy = Math.Abs(Y - other.Y);
            if (dx < dy)
                return dx + dy - (dx / 2);
            return dx + dy - (dy / 2);
        }
        public readonly double ApproximateDistance2D(Vector3D other)
        {
            double dx = Math.Abs(X - other.X);
            double dy = Math.Abs(Y - other.Y);
            if (dx < dy)
                return dx + dy - (dx / 2);
            return dx + dy - (dy / 2);
        }

        public readonly double ApproximateExplosionDistance2D(in Vec3D other)
        {
            double dx = Math.Abs(X - other.X);
            double dy = Math.Abs(Y - other.Y);
            return dx > dy ? dx : dy;
        }

        public override readonly string ToString() => $"{X}, {Y}, {Z}";
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);
        public readonly bool Equals(Vec3D other) => X == other.X && Y == other.Y && Z == other.Z;
        public readonly override bool Equals(object? obj) => obj is not null && obj is Vec3D v && Equals(v);
    }
}
