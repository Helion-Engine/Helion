﻿// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GlmSharp;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Util.Extensions;

namespace Helion.Geometry.Vectors
{
    public class Vector3D
    {
        public static readonly Vector3D Zero = new(0, 0, 0);
        public static readonly Vector3D One = new(1, 1, 1);

        public double X;
        public double Y;
        public double Z;

        public double U => X;
        public double V => Y;
        public Vec2D XY => new(X, Y);
        public Vec2D XZ => new(X, Z);
        public Vec3I Int => new((int)X, (int)Y, (int)Z);
        public Vec3F Float => new((float)X, (float)Y, (float)Z);
        public Vec3Fixed FixedPoint => new(Fixed.From(X), Fixed.From(Y), Fixed.From(Z));
        public Box3D Box => new((0, 0, 0), (X, Y, Z));
        public Vec3D Struct => new(X, Y, Z);
        public IEnumerable<double> Values => GetEnumerableValues();

        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Deconstruct(out double x, out double y, out double z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public double this[int index]
        {
            get
            {
                return index switch
                {
                    0 => X,
                    1 => Y,
                    2 => Z,
                    _ => throw new IndexOutOfRangeException()
                }
                ;
            }
            set
            {
                switch (index)
                {
                    case 0:
                        X = value;
                        break;
                    case 1:
                        Y = value;
                        break;
                    case 2:
                        Z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public static Vec3D operator -(Vector3D self) => new(-self.X, -self.Y, -self.Z);
        public static Vec3D operator +(Vector3D self, Vec3D other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3D operator +(Vector3D self, Vector3D other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3D operator -(Vector3D self, Vec3D other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3D operator -(Vector3D self, Vector3D other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3D operator *(Vector3D self, Vec3D other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3D operator *(Vector3D self, Vector3D other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3D operator *(Vector3D self, double value) => new(self.X * value, self.Y * value, self.Z * value);
        public static Vec3D operator *(double value, Vector3D self) => new(self.X * value, self.Y * value, self.Z * value);
        public static Vec3D operator /(Vector3D self, Vec3D other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3D operator /(Vector3D self, Vector3D other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3D operator /(Vector3D self, double value) => new(self.X / value, self.Y / value, self.Z / value);

        public Vec3D WithX(double x) => new(x, Y, Z);
        public Vec3D WithY(double y) => new(X, y, Z);
        public Vec3D WithZ(double z) => new(X, Y, z);
        public bool IsApprox(Vec3D other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y) && Z.ApproxEquals(other.Z);
        public bool IsApprox(Vector3D other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y) && Z.ApproxEquals(other.Z);
        public Vec4D To4D(double w) => new(X, Y, Z, w);

        public Vec3D Abs() => new(X.Abs(), Y.Abs(), Z.Abs());
        public Vec3D Floor() => new(X.Floor(), Y.Floor(), Z.Floor());
        public Vec3D Ceiling() => new(X.Ceiling(), Y.Ceiling(), Z.Ceiling());
        public Vec3D Unit() => this / Length();
        public void Normalize()
        {
            double len = Length();
            X /= len;
            Y /= len;
            Z /= len;
        }
        public double LengthSquared() => (X * X) + (Y * Y) + (Z * Z);
        public Vec3D Inverse() => new(1 / X, 1 / Y, 1 / Z);
        public double Length() => Math.Sqrt(LengthSquared());
        public double DistanceSquared(Vec3D other) => (this - other).LengthSquared();
        public double DistanceSquared(Vector3D other) => (this - other).LengthSquared();
        public double Distance(Vec3D other) => (this - other).Length();
        public double Distance(Vector3D other) => (this - other).Length();
        public Vec3D Interpolate(Vec3D end, double t) => this + (t * (end - this));
        public Vec3D Interpolate(Vector3D end, double t) => this + (t * (end - this));
        public double Dot(Vec3D other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public double Dot(Vector3D other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public static Vec3D UnitSphere(double angle, double pitch)
        {
            double sinAngle = Math.Sin(angle);
            double cosAngle = Math.Cos(angle);
            double sinPitch = Math.Sin(pitch);
            double cosPitch = Math.Cos(pitch);
            return new(cosAngle * cosPitch, sinAngle * cosPitch, sinPitch);
        }
        public Vec3D Rotate2D(double yawRadians)
        {
            double sin = Math.Sin(yawRadians);
            double cos = Math.Cos(yawRadians);
            return new((X * cos) - (Y * sin), (X * sin) + (Y * cos), Z);
        }
        public double Pitch(in Vec3D other, double length) => Math.Atan2(other.Z - Z, length);
        public double Pitch(Vector3D other, double length) => Math.Atan2(other.Z - Z, length);
        public double Pitch(double z, double length) => Math.Atan2(z - Z, length);
        public double Angle(in Vec3D other) => Math.Atan2(other.Y - Y, other.X - X);
        public double Angle(Vector3D other) => Math.Atan2(other.Y - Y, other.X - X);
        public double Angle(in Vec2D other) => Math.Atan2(other.Y - Y, other.X - X);
        public double Angle(Vector2D other) => Math.Atan2(other.Y - Y, other.X - X);
        public double ApproximateDistance2D(in Vec3D other)
        {
            double dx = Math.Abs(X - other.X);
            double dy = Math.Abs(Y - other.Y);
            if (dx < dy)
                return dx + dy - (dx / 2);
            return dx + dy - (dy / 2);
        }
        public double ApproximateDistance2D(Vector3D other)
        {
            double dx = Math.Abs(X - other.X);
            double dy = Math.Abs(Y - other.Y);
            if (dx < dy)
                return dx + dy - (dx / 2);
            return dx + dy - (dy / 2);
        }

        private IEnumerable<double> GetEnumerableValues()
        {
            yield return X;
            yield return Y;
            yield return Z;
        }

        public override string ToString() => $"{X}, {Y}, {Z}";
        public override bool Equals(object? obj) => obj is Vector3D v && X == v.X && Y == v.Y && Z == v.Z;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }
}
