// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using GlmSharp;
using Helion.Geometry.Boxes;
using Helion.Util.Configs.Impl;
using Helion.Util.Extensions;

namespace Helion.Geometry.Vectors
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vec3F(float x, float y, float z)
    {
        public static readonly Vec3F Zero = new(0, 0, 0);
        public static readonly Vec3F One = new(1, 1, 1);

        public float X = x;
        public float Y = y;
        public float Z = z;

        public readonly Vec2F XY => new(X, Y);
        public readonly Vec2F XZ => new(X, Z);
        public readonly Vec3I Int => new((int)X, (int)Y, (int)Z);
        public readonly Vec3D Double => new((double)X, (double)Y, (double)Z);
        public readonly Vec3Fixed FixedPoint => new(Fixed.From(X), Fixed.From(Y), Fixed.From(Z));
        public readonly Box3F Box => new((0, 0, 0), (X, Y, Z));
        public readonly vec3 GlmVector => new(X, Y, Z);

        public static implicit operator Vec3F(ValueTuple<float, float, float> tuple)
        {
            return new(tuple.Item1, tuple.Item2, tuple.Item3);
        }

        public static Vec3F operator -(Vec3F self) => new(-self.X, -self.Y, -self.Z);
        public static Vec3F operator +(Vec3F self, Vec3F other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3F operator +(Vec3F self, Vector3F other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3F operator -(Vec3F self, Vec3F other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3F operator -(Vec3F self, Vector3F other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3F operator *(Vec3F self, Vec3F other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3F operator *(Vec3F self, Vector3F other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3F operator *(Vec3F self, float value) => new(self.X * value, self.Y * value, self.Z * value);
        public static Vec3F operator *(float value, Vec3F self) => new(self.X * value, self.Y * value, self.Z * value);
        public static Vec3F operator /(Vec3F self, Vec3F other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3F operator /(Vec3F self, Vector3F other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3F operator /(Vec3F self, float value) => new(self.X / value, self.Y / value, self.Z / value);
        public static bool operator ==(Vec3F self, Vec3F other) => self.X == other.X && self.Y == other.Y && self.Z == other.Z;
        public static bool operator !=(Vec3F self, Vec3F other) => !(self == other);

        public readonly Vec3F WithX(float x) => new(x, Y, Z);
        public readonly Vec3F WithY(float y) => new(X, y, Z);
        public readonly Vec3F WithZ(float z) => new(X, Y, z);
        public readonly bool IsApprox(Vec3F other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y) && Z.ApproxEquals(other.Z);
        public readonly bool IsApprox(Vector3F other) => X.ApproxEquals(other.X) && Y.ApproxEquals(other.Y) && Z.ApproxEquals(other.Z);
        public readonly Vec4F To4D(float w) => new(X, Y, Z, w);

        public readonly Vec3F Abs() => new(X.Abs(), Y.Abs(), Z.Abs());
        public readonly Vec3F Floor() => new(X.Floor(), Y.Floor(), Z.Floor());
        public readonly Vec3F Ceiling() => new(X.Ceiling(), Y.Ceiling(), Z.Ceiling());
        public readonly Vec3F Unit() => this / Length();
        public void Normalize() => this /= Length();
        public readonly float LengthSquared() => (X * X) + (Y * Y) + (Z * Z);
        public readonly Vec3F Inverse() => new(1 / X, 1 / Y, 1 / Z);
        public readonly float Length() => MathF.Sqrt(LengthSquared());
        public readonly float DistanceSquared(Vec3F other) => (this - other).LengthSquared();
        public readonly float DistanceSquared(Vector3F other) => (this - other).LengthSquared();
        public readonly float Distance(Vec3F other) => (this - other).Length();
        public readonly float Distance(Vector3F other) => (this - other).Length();
        public readonly Vec3F Interpolate(Vec3F end, float t) => this + (t * (end - this));
        public readonly Vec3F Interpolate(Vector3F end, float t) => this + (t * (end - this));
        public readonly float Dot(Vec3F other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public readonly float Dot(Vector3F other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public static Vec3F UnitSphere(float angle, float pitch)
        {
            float sinAngle = MathF.Sin(angle);
            float cosAngle = MathF.Cos(angle);
            float sinPitch = MathF.Sin(pitch);
            float cosPitch = MathF.Cos(pitch);
            return new(cosAngle * cosPitch, sinAngle * cosPitch, sinPitch);
        }
        public readonly Vec3F Rotate2D(float yawRadians)
        {
            float sin = MathF.Sin(yawRadians);
            float cos = MathF.Cos(yawRadians);
            return new((X * cos) - (Y * sin), (X * sin) + (Y * cos), Z);
        }
        public readonly float Pitch(in Vec3F other, float length) => MathF.Atan2(other.Z - Z, length);
        public readonly float Pitch(Vector3F other, float length) => MathF.Atan2(other.Z - Z, length);
        public readonly float Pitch(float z, float length) => MathF.Atan2(z - Z, length);
        public readonly float Angle(in Vec3F other) => MathF.Atan2(other.Y - Y, other.X - X);
        public readonly float Angle(Vector3F other) => MathF.Atan2(other.Y - Y, other.X - X);
        public readonly float Angle(in Vec2F other) => MathF.Atan2(other.Y - Y, other.X - X);
        public readonly float Angle(Vector2F other) => MathF.Atan2(other.Y - Y, other.X - X);
        public readonly float ApproximateDistance2D(in Vec3F other)
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

        public override readonly string ToString() => $"{X}, {Y}, {Z}";
        public override readonly bool Equals(object? obj) => obj is Vec3F v && X == v.X && Y == v.Y && Z == v.Z;
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);

        public static Vec3F FromConfigString(string s)
        {
            try
            {
                var tokens = s.Split(Config.FindSplitValue(s));
                var x = float.Parse(tokens[0].Trim(), CultureInfo.InvariantCulture);
                var y = float.Parse(tokens[1].Trim(), CultureInfo.InvariantCulture);
                var z = float.Parse(tokens[2].Trim(), CultureInfo.InvariantCulture);
                return (x, y, z);
            }
            catch
            {
                return (1, 1, 1);
            }
        }
    }
}
