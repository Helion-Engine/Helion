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
    public struct Vec2D
    {
        public static readonly Vec2D Zero = (0, 0);
        public static readonly Vec2D One = (1, 1);

        public double X;
        public double Y;

        public double U => X;
        public double V => Y;
        public Vec2I Int => new((int)X, (int)Y);
        public Vec2F Float => new((float)X, (float)Y);
        public Vec2Fixed FixedPoint => new(Fixed.From(X), Fixed.From(Y));
        public IEnumerable<double> Values => GetEnumerableValues();

        public Vec2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator Vec2D(ValueTuple<double, double> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public void Deconstruct(out double x, out double y)
        {
            x = X;
            y = Y;
        }

        public double this[int index]
        {
            get
            {
                return index switch
                {
                    0 => X,
                    1 => Y,
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
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
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
        public static bool operator ==(Vec2D self, Vector2D other) => self.X == other.X && self.Y == other.Y;
        public static bool operator !=(Vec2D self, Vec2D other) => !(self == other);
        public static bool operator !=(Vec2D self, Vector2D other) => !(self == other);

        public Vec2D WithX(double x) => new(x, Y);
        public Vec2D WithY(double y) => new(X, y);
        public bool IsApprox(Vec2D other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y);
        public bool IsApprox(Vector2D other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y);
        public Vec3D To3D(double z) => new(X, Y, z);

        public Vec2D Abs() => new(X.Abs(), Y.Abs());
        public Vec2D Floor() => new(X.Floor(), Y.Floor());
        public Vec2D Ceiling() => new(X.Ceiling(), Y.Ceiling());
        public Vec2D Unit() => this / Length();
        public void Normalize() => this /= Length();
        public double LengthSquared() => (X * X) + (Y * Y);
        public Vec2D Inverse() => new(1 / X, 1 / Y);
        public Rotation Rotation(Vec2D second, Vec2D third, double epsilon = 0.000001) => new Seg2D(this, second).ToSide(third, epsilon);
        public Rotation Rotation(Vector2D second, Vector2D third, double epsilon = 0.000001) => new Seg2D(this, second).ToSide(third, epsilon);
        public double Length() => Math.Sqrt(LengthSquared());
        public double DistanceSquared(Vec2D other) => (this - other).LengthSquared();
        public double DistanceSquared(Vector2D other) => (this - other).LengthSquared();
        public double Distance(Vec2D other) => (this - other).Length();
        public double Distance(Vector2D other) => (this - other).Length();
        public Vec2D Interpolate(Vec2D end, double t) => this + (t * (end - this));
        public Vec2D Interpolate(Vector2D end, double t) => this + (t * (end - this));
        public double Dot(Vec2D other) => (X * other.X) + (Y * other.Y);
        public double Dot(Vector2D other) => (X * other.X) + (Y * other.Y);
        public double Component(Vec2D onto) => Dot(onto) / onto.Length();
        public double Component(Vector2D onto) => Dot(onto) / onto.Length();
        public Vec2D Projection(Vec2D onto) => Dot(onto) / onto.LengthSquared() * onto;
        public Vec2D Projection(Vector2D onto) => Dot(onto) / onto.LengthSquared() * onto;
        public Vec2D RotateRight90() => new(Y, -X);
        public Vec2D RotateLeft90() => new(-Y, X);
        public static Vec2D UnitCircle(double radians) => new(Math.Cos(radians), Math.Sin(radians));

        private IEnumerable<double> GetEnumerableValues()
        {
            yield return X;
            yield return Y;
        }

        public override string ToString() => $"{X}, {Y}";
        public override bool Equals(object? obj) => obj is Vec2D v && X == v.X && Y == v.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }
}
