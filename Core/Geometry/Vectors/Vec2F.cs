// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using GlmSharp;
using Helion.Util.Extensions;
using Helion.Util.Geometry;

namespace Helion.Geometry.Vectors
{
    public struct Vec2F
    {
        public static readonly Vec2F Zero = (0, 0);
        public static readonly Vec2F One = (1, 1);

        public float X;
        public float Y;

        public float U => X;
        public float V => Y;
        public Vec2I Int => new((int)X, (int)Y);
        public Vec2D Double => new((double)X, (double)Y);
        public Vec2Fixed FixedPoint => new(Fixed.From(X), Fixed.From(Y));
        public IEnumerable<float> Values => GetEnumerableValues();

        public Vec2F(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator Vec2F(ValueTuple<float, float> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public void Deconstruct(out float x, out float y)
        {
            x = X;
            y = Y;
        }

        public float this[int index]
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

        public static Vec2F operator -(Vec2F self) => new(-self.X, -self.Y);
        public static Vec2F operator +(Vec2F self, Vec2F other) => new(self.X + other.X, self.Y + other.Y);
        public static Vec2F operator +(Vec2F self, Vector2F other) => new(self.X + other.X, self.Y + other.Y);
        public static Vec2F operator -(Vec2F self, Vec2F other) => new(self.X - other.X, self.Y - other.Y);
        public static Vec2F operator -(Vec2F self, Vector2F other) => new(self.X - other.X, self.Y - other.Y);
        public static Vec2F operator *(Vec2F self, Vec2F other) => new(self.X * other.X, self.Y * other.Y);
        public static Vec2F operator *(Vec2F self, Vector2F other) => new(self.X * other.X, self.Y * other.Y);
        public static Vec2F operator *(Vec2F self, float value) => new(self.X * value, self.Y * value);
        public static Vec2F operator *(float value, Vec2F self) => new(self.X * value, self.Y * value);
        public static Vec2F operator /(Vec2F self, Vec2F other) => new(self.X / other.X, self.Y / other.Y);
        public static Vec2F operator /(Vec2F self, Vector2F other) => new(self.X / other.X, self.Y / other.Y);
        public static Vec2F operator /(Vec2F self, float value) => new(self.X / value, self.Y / value);
        public static bool operator ==(Vec2F self, Vec2F other) => self.X == other.X && self.Y == other.Y;
        public static bool operator ==(Vec2F self, Vector2F other) => self.X == other.X && self.Y == other.Y;
        public static bool operator !=(Vec2F self, Vec2F other) => !(self == other);
        public static bool operator !=(Vec2F self, Vector2F other) => !(self == other);

        public Vec2F WithX(float x) => new(x, Y);
        public Vec2F WithY(float y) => new(X, y);
        public bool IsApprox(Vec2F other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y);
        public bool IsApprox(Vector2F other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y);
        public Vec3F To3D(float z) => new(X, Y, z);

        public Vec2F Abs() => new(X.Abs(), Y.Abs());
        public Vec2F Floor() => new(X.Floor(), Y.Floor());
        public Vec2F Ceiling() => new(X.Ceiling(), Y.Ceiling());
        public Vec2F Unit() => this / Length();
        public void Normalize() => this /= Length();
        public float LengthSquared() => (X * X) + (Y * Y);
        public float Length() => MathF.Sqrt(LengthSquared());
        public float DistanceSquared(Vec2F other) => (this - other).LengthSquared();
        public float DistanceSquared(Vector2F other) => (this - other).LengthSquared();
        public float Distance(Vec2F other) => (this - other).Length();
        public float Distance(Vector2F other) => (this - other).Length();
        public Vec2F Interpolate(Vec2F end, float t) => this + (t * (end - this));
        public Vec2F Interpolate(Vector2F end, float t) => this + (t * (end - this));
        public float Dot(Vec2F other) => (X * other.X) + (Y * other.Y);
        public float Dot(Vector2F other) => (X * other.X) + (Y * other.Y);
        public float Component(Vec2F onto) => Dot(onto) / onto.Length();
        public float Component(Vector2F onto) => Dot(onto) / onto.Length();
        public Vec2F Projection(Vec2F onto) => Dot(onto) / onto.LengthSquared() * onto;
        public Vec2F Projection(Vector2F onto) => Dot(onto) / onto.LengthSquared() * onto;
        public Vec2F RotateRight90() => new(Y, -X);
        public Vec2F RotateLeft90() => new(-Y, X);
        public static Vec2F UnitCircle(float radians) => new(MathF.Cos(radians), MathF.Sin(radians));

        private IEnumerable<float> GetEnumerableValues()
        {
            yield return X;
            yield return Y;
        }

        public override string ToString() => $"{X}, {Y}";
        public override bool Equals(object? obj) => obj is Vec2F v && X == v.X && Y == v.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }
}
