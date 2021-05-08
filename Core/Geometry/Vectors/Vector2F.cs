// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using GlmSharp;
using Helion.Geometry.Segments;
using Helion.Util.Extensions;

namespace Helion.Geometry.Vectors
{
    public class Vector2F
    {
        public static readonly Vector2F Zero = new(0, 0);
        public static readonly Vector2F One = new(1, 1);

        public float X;
        public float Y;

        public float U => X;
        public float V => Y;
        public Vec2I Int => new((int)X, (int)Y);
        public Vec2D Double => new((double)X, (double)Y);
        public Vec2Fixed FixedPoint => new(Fixed.From(X), Fixed.From(Y));
        public Vec2F Struct => new(X, Y);
        public IEnumerable<float> Values => GetEnumerableValues();

        public Vector2F(float x, float y)
        {
            X = x;
            Y = y;
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

        public static Vec2F operator -(Vector2F self) => new(-self.X, -self.Y);
        public static Vec2F operator +(Vector2F self, Vec2F other) => new(self.X + other.X, self.Y + other.Y);
        public static Vec2F operator +(Vector2F self, Vector2F other) => new(self.X + other.X, self.Y + other.Y);
        public static Vec2F operator -(Vector2F self, Vec2F other) => new(self.X - other.X, self.Y - other.Y);
        public static Vec2F operator -(Vector2F self, Vector2F other) => new(self.X - other.X, self.Y - other.Y);
        public static Vec2F operator *(Vector2F self, Vec2F other) => new(self.X * other.X, self.Y * other.Y);
        public static Vec2F operator *(Vector2F self, Vector2F other) => new(self.X * other.X, self.Y * other.Y);
        public static Vec2F operator *(Vector2F self, float value) => new(self.X * value, self.Y * value);
        public static Vec2F operator *(float value, Vector2F self) => new(self.X * value, self.Y * value);
        public static Vec2F operator /(Vector2F self, Vec2F other) => new(self.X / other.X, self.Y / other.Y);
        public static Vec2F operator /(Vector2F self, Vector2F other) => new(self.X / other.X, self.Y / other.Y);
        public static Vec2F operator /(Vector2F self, float value) => new(self.X / value, self.Y / value);

        public Vec2F WithX(float x) => new(x, Y);
        public Vec2F WithY(float y) => new(X, y);
        public bool IsApprox(Vec2F other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y);
        public bool IsApprox(Vector2F other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y);
        public Vec3F To3D(float z) => new(X, Y, z);

        public Vec2F Abs() => new(X.Abs(), Y.Abs());
        public Vec2F Floor() => new(X.Floor(), Y.Floor());
        public Vec2F Ceiling() => new(X.Ceiling(), Y.Ceiling());
        public Vec2F Unit() => this / Length();
        public void Normalize()
        {
            float len = Length();
            X /= len;
            Y /= len;
        }
        public float LengthSquared() => (X * X) + (Y * Y);
        public Vec2F Inverse() => new(1 / X, 1 / Y);
        public Rotation Rotation(Vec2F second, Vec2F third, float epsilon = 0.0001f) => new Seg2F(this, second).ToSide(third, epsilon);
        public Rotation Rotation(Vector2F second, Vector2F third, float epsilon = 0.0001f) => new Seg2F(this, second).ToSide(third, epsilon);
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
        public float Angle(in Vec2F other) => MathF.Atan2(other.Y - Y, other.X - X);
        public float Angle(Vector2F other) => MathF.Atan2(other.Y - Y, other.X - X);
        public float Angle(in Vec3F other) => MathF.Atan2(other.Y - Y, other.X - X);
        public float Angle(Vector3F other) => MathF.Atan2(other.Y - Y, other.X - X);

        private IEnumerable<float> GetEnumerableValues()
        {
            yield return X;
            yield return Y;
        }

        public override string ToString() => $"{X}, {Y}";
        public override bool Equals(object? obj) => obj is Vector2F v && X == v.X && Y == v.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }
}
