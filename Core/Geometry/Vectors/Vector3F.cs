// THIS FILE WAS AUTO-GENERATED.
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
    public class Vector3F
    {
        public static readonly Vector3F Zero = new(0, 0, 0);
        public static readonly Vector3F One = new(1, 1, 1);

        public float X;
        public float Y;
        public float Z;

        public float U => X;
        public float V => Y;
        public Vec2F XY => new(X, Y);
        public Vec2F XZ => new(X, Z);
        public Vec3I Int => new((int)X, (int)Y, (int)Z);
        public Vec3D Double => new((double)X, (double)Y, (double)Z);
        public Vec3Fixed FixedPoint => new(Fixed.From(X), Fixed.From(Y), Fixed.From(Z));
        public Box3F Box => new((0, 0, 0), (X, Y, Z));
        public vec3 GlmVector => new(X, Y, Z);
        public Vec3F Struct => new(X, Y, Z);
        public IEnumerable<float> Values => GetEnumerableValues();

        public Vector3F(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Deconstruct(out float x, out float y, out float z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public float this[int index]
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

        public static Vec3F operator -(Vector3F self) => new(-self.X, -self.Y, -self.Z);
        public static Vec3F operator +(Vector3F self, Vec3F other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3F operator +(Vector3F self, Vector3F other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3F operator -(Vector3F self, Vec3F other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3F operator -(Vector3F self, Vector3F other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3F operator *(Vector3F self, Vec3F other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3F operator *(Vector3F self, Vector3F other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3F operator *(Vector3F self, float value) => new(self.X * value, self.Y * value, self.Z * value);
        public static Vec3F operator *(float value, Vector3F self) => new(self.X * value, self.Y * value, self.Z * value);
        public static Vec3F operator /(Vector3F self, Vec3F other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3F operator /(Vector3F self, Vector3F other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3F operator /(Vector3F self, float value) => new(self.X / value, self.Y / value, self.Z / value);

        public Vec3F WithX(float x) => new(x, Y, Z);
        public Vec3F WithY(float y) => new(X, y, Z);
        public Vec3F WithZ(float z) => new(X, Y, z);
        public bool IsApprox(Vec3F other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y) && Z.ApproxEquals(other.Z);
        public bool IsApprox(Vector3F other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y) && Z.ApproxEquals(other.Z);
        public Vec4F To4D(float w) => new(X, Y, Z, w);

        public Vec3F Abs() => new(X.Abs(), Y.Abs(), Z.Abs());
        public Vec3F Floor() => new(X.Floor(), Y.Floor(), Z.Floor());
        public Vec3F Ceiling() => new(X.Ceiling(), Y.Ceiling(), Z.Ceiling());
        public Vec3F Unit() => this / Length();
        public void Normalize()
        {
            float len = Length();
            X /= len;
            Y /= len;
            Z /= len;
        }
        public float LengthSquared() => (X * X) + (Y * Y) + (Z * Z);
        public Vec3F Inverse() => new(1 / X, 1 / Y, 1 / Z);
        public float Length() => MathF.Sqrt(LengthSquared());
        public float DistanceSquared(Vec3F other) => (this - other).LengthSquared();
        public float DistanceSquared(Vector3F other) => (this - other).LengthSquared();
        public float Distance(Vec3F other) => (this - other).Length();
        public float Distance(Vector3F other) => (this - other).Length();
        public Vec3F Interpolate(Vec3F end, float t) => this + (t * (end - this));
        public Vec3F Interpolate(Vector3F end, float t) => this + (t * (end - this));
        public float Dot(Vec3F other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public float Dot(Vector3F other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public static Vec3F UnitSphere(float angle, float pitch)
        {
            float sinAngle = MathF.Sin(angle);
            float cosAngle = MathF.Cos(angle);
            float sinPitch = MathF.Sin(pitch);
            float cosPitch = MathF.Cos(pitch);
            return new(cosAngle * cosPitch, sinAngle * cosPitch, sinPitch);
        }
        public Vec3F Rotate2D(float yawRadians)
        {
            float sin = MathF.Sin(yawRadians);
            float cos = MathF.Cos(yawRadians);
            return new((X * cos) - (Y * sin), (X * sin) + (Y * cos), Z);
        }
        public float Pitch(in Vec3F other, float length) => MathF.Atan2(other.Z - Z, length);
        public float Pitch(Vector3F other, float length) => MathF.Atan2(other.Z - Z, length);
        public float Pitch(float z, float length) => MathF.Atan2(z - Z, length);
        public float Angle(in Vec3F other) => MathF.Atan2(other.Y - Y, other.X - X);
        public float Angle(Vector3F other) => MathF.Atan2(other.Y - Y, other.X - X);
        public float Angle(in Vec2F other) => MathF.Atan2(other.Y - Y, other.X - X);
        public float Angle(Vector2F other) => MathF.Atan2(other.Y - Y, other.X - X);
        public float ApproximateDistance2D(in Vec3F other)
        {
            float dx = MathF.Abs(X - other.X);
            float dy = MathF.Abs(Y - other.Y);
            if (dx < dy)
                return dx + dy - (dx / 2);
            return dx + dy - (dy / 2);
        }
        public float ApproximateDistance2D(Vector3F other)
        {
            float dx = MathF.Abs(X - other.X);
            float dy = MathF.Abs(Y - other.Y);
            if (dx < dy)
                return dx + dy - (dx / 2);
            return dx + dy - (dy / 2);
        }

        private IEnumerable<float> GetEnumerableValues()
        {
            yield return X;
            yield return Y;
            yield return Z;
        }

        public override string ToString() => $"{X}, {Y}, {Z}";
        public override bool Equals(object? obj) => obj is Vector3F v && X == v.X && Y == v.Y && Z == v.Z;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }
}
