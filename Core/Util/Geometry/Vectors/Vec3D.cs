using System;
using System.Diagnostics.Contracts;
using System.Numerics;

namespace Helion.Util.Geometry.Vectors
{
    public struct Vec3D
    {
        public static readonly Vec3D Zero = new Vec3D(0, 0, 0);

        public double X;
        public double Y;
        public double Z;

        public Vec3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3D operator +(Vec3D self, Vec3D other) => new Vec3D(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3D operator -(Vec3D self, Vec3D other) => new Vec3D(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3D operator *(Vec3D self, Vec3D other) => new Vec3D(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3D operator *(Vec3D self, double value) => new Vec3D(self.X * value, self.Y * value, self.Z * value);
        public static Vec3D operator *(double value, Vec3D self) => new Vec3D(self.X * value, self.Y * value, self.Z * value);
        public static Vec3D operator /(Vec3D self, Vec3D other) => new Vec3D(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3D operator /(Vec3D self, double value) => new Vec3D(self.X / value, self.Y / value, self.Z / value);
        public static Vec3D operator /(double value, Vec3D self) => new Vec3D(self.X / value, self.Y / value, self.Z / value);
        public static bool operator !=(Vec3D self, Vec3D other) => !(self == other);

        public static bool operator ==(Vec3D self, Vec3D other)
        {
            return MathHelper.AreEqual(self.X, other.X) && MathHelper.AreEqual(self.Y, other.Y) && MathHelper.AreEqual(self.Z, other.Z);
        }

        public static Vec3D UnitTimesValue(double angle, double pitch, double value)
        {
            return new Vec3D(Math.Cos(angle) * Math.Cos(pitch) * value, Math.Sin(angle) * Math.Cos(pitch) * value, Math.Sin(pitch) * value);
        }

        public static Vec3D Unit(double angle, double pitch)
        {
            return new Vec3D(Math.Cos(angle) * Math.Cos(pitch), Math.Sin(angle) * Math.Cos(pitch), Math.Sin(pitch));
        }

        public bool EqualTo(Vec3D other, double epsilon = 0.00001)
        {
            return MathHelper.AreEqual(X, other.X, epsilon) && MathHelper.AreEqual(Y, other.Y, epsilon) && MathHelper.AreEqual(Z, other.Z, epsilon);
        }

        public double this[int index]
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

        [Pure] public Vec3D Abs() => new(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
        [Pure] public Vec3D Unit() => this / Length();
        public void Normalize() => this /= Length();
        [Pure] public double Dot(in Vec3D other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        [Pure] public double LengthSquared() => (X * X) + (Y * Y) + (Z * Z);
        [Pure] public double Length() => Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        [Pure] public double DistanceSquared(Vec3D other) => (this - other).LengthSquared();
        [Pure] public double Distance(in Vec3D other) => (this - other).Length();
        [Pure] public Vec3D Interpolate(Vec3D end, double t) => this + (t * (end - this));
        [Pure] public double Pitch(in Vec3D other, double length) => Math.Atan2(other.Z - Z, length);
        [Pure] public double Pitch(double z, double length) => Math.Atan2(z - Z, length);
        [Pure] public double Angle(in Vec3D other) => Math.Atan2(other.Y - Y, other.X - X);
        [Pure] public double Angle(in Vec2D other) => Math.Atan2(other.Y - Y, other.X - X);

        public void Multiply(double value)
        {
            X *= value;
            Y *= value;
            Z *= value;
        }

        [Pure]
        public double ApproximateDistance2D(in Vec3D other)
        {
            double dx = Math.Abs(X - other.X);
            double dy = Math.Abs(Y - other.Y);

            if (dx < dy)
                return dx + dy - (dx / 2);
            return dx + dy - (dy / 2);
        }

        [Pure] public Vec2D To2D() => new(X, Y);
        [Pure] public Vec3Fixed ToFixed() => new(new Fixed(X), new Fixed(Y), new Fixed(Z));
        [Pure] public Vector3 ToFloat() => new((float)X, (float)Y, (float)Z);
        [Pure] public Vec3I ToInt() => new((int)X, (int)Y, (int)Z);

        public override string ToString() => $"{X}, {Y}, {Z}";
        public override bool Equals(object? obj) => obj is Vec3D v && X == v.X && Y == v.Y && Z == v.Z;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }
}