// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using GlmSharp;
using Helion.Geometry.Segments;
using Helion.Geometry.Segments.Enums;
using Helion.Util.Extensions;

namespace Helion.Geometry.Vectors
{
    public struct Vec4D
    {
        public static readonly Vec4D Zero = (0, 0, 0, 0);
        public static readonly Vec4D One = (1, 1, 1, 1);

        public double X;
        public double Y;
        public double Z;
        public double W;

        public double U => X;
        public double V => Y;
        public Vec2D XY => new(X, Y);
        public Vec2D XZ => new(X, Z);
        public Vec3D XYZ => new(X, Y, Z);
        public Vec4I Int => new((int)X, (int)Y, (int)Z, (int)W);
        public Vec4F Float => new((float)X, (float)Y, (float)Z, (float)W);
        public Vec4Fixed FixedPoint => new(Fixed.From(X), Fixed.From(Y), Fixed.From(Z), Fixed.From(W));
        public IEnumerable<double> Values => GetEnumerableValues();

        public Vec4D(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static implicit operator Vec4D(ValueTuple<double, double, double, double> tuple)
        {
            return new(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
        }

        public void Deconstruct(out double x, out double y, out double z, out double w)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
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
                    3 => W,
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
                    case 3:
                        W = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public static Vec4D operator -(Vec4D self) => new(-self.X, -self.Y, -self.Z, -self.W);
        public static Vec4D operator +(Vec4D self, Vec4D other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z, self.W + other.W);
        public static Vec4D operator +(Vec4D self, Vector4D other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z, self.W + other.W);
        public static Vec4D operator -(Vec4D self, Vec4D other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z, self.W - other.W);
        public static Vec4D operator -(Vec4D self, Vector4D other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z, self.W - other.W);
        public static Vec4D operator *(Vec4D self, Vec4D other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z, self.W * other.W);
        public static Vec4D operator *(Vec4D self, Vector4D other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z, self.W * other.W);
        public static Vec4D operator *(Vec4D self, double value) => new(self.X * value, self.Y * value, self.Z * value, self.W * value);
        public static Vec4D operator *(double value, Vec4D self) => new(self.X * value, self.Y * value, self.Z * value, self.W * value);
        public static Vec4D operator /(Vec4D self, Vec4D other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z, self.W / other.W);
        public static Vec4D operator /(Vec4D self, Vector4D other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z, self.W / other.W);
        public static Vec4D operator /(Vec4D self, double value) => new(self.X / value, self.Y / value, self.Z / value, self.W / value);
        public static bool operator ==(Vec4D self, Vec4D other) => self.X == other.X && self.Y == other.Y && self.Z == other.Z && self.W == other.W;
        public static bool operator ==(Vec4D self, Vector4D other) => self.X == other.X && self.Y == other.Y && self.Z == other.Z && self.W == other.W;
        public static bool operator !=(Vec4D self, Vec4D other) => !(self == other);
        public static bool operator !=(Vec4D self, Vector4D other) => !(self == other);

        public Vec4D WithX(double x) => new(x, Y, Z, W);
        public Vec4D WithY(double y) => new(X, y, Z, W);
        public Vec4D WithZ(double z) => new(X, Y, z, W);
        public Vec4D WithW(double w) => new(X, Y, Z, w);
        public bool IsApprox(Vec4D other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y) && Z.ApproxEquals(other.Z) && W.ApproxEquals(other.W);
        public bool IsApprox(Vector4D other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y) && Z.ApproxEquals(other.Z) && W.ApproxEquals(other.W);

        public Vec4D Abs() => new(X.Abs(), Y.Abs(), Z.Abs(), W.Abs());
        public Vec4D Floor() => new(X.Floor(), Y.Floor(), Z.Floor(), W.Floor());
        public Vec4D Ceiling() => new(X.Ceiling(), Y.Ceiling(), Z.Ceiling(), W.Ceiling());
        public Vec4D Unit() => this / Length();
        public void Normalize() => this /= Length();
        public double LengthSquared() => (X * X) + (Y * Y) + (Z * Z) + (W * W);
        public Vec4D Inverse() => new(1 / X, 1 / Y, 1 / Z, 1 / W);
        public double Length() => Math.Sqrt(LengthSquared());
        public double DistanceSquared(Vec4D other) => (this - other).LengthSquared();
        public double DistanceSquared(Vector4D other) => (this - other).LengthSquared();
        public double Distance(Vec4D other) => (this - other).Length();
        public double Distance(Vector4D other) => (this - other).Length();
        public Vec4D Interpolate(Vec4D end, double t) => this + (t * (end - this));
        public Vec4D Interpolate(Vector4D end, double t) => this + (t * (end - this));
        public double Dot(Vec4D other) => (X * other.X) + (Y * other.Y) + (Z * other.Z) + (W * other.W);
        public double Dot(Vector4D other) => (X * other.X) + (Y * other.Y) + (Z * other.Z) + (W * other.W);

        private IEnumerable<double> GetEnumerableValues()
        {
            yield return X;
            yield return Y;
            yield return Z;
            yield return W;
        }

        public override string ToString() => $"{X}, {Y}, {Z}, {W}";
        public override bool Equals(object? obj) => obj is Vec4D v && X == v.X && Y == v.Y && Z == v.Z && W == v.W;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    }
}
