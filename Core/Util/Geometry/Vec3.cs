using System;
using System.Numerics;

namespace Helion.Util.Geometry
{
    public struct Vec3i
    {
        public int X;
        public int Y;
        public int Z;

        public Vec3i(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3i operator +(Vec3i self, Vec3i other) => new Vec3i(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3i operator -(Vec3i self, Vec3i other) => new Vec3i(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3i operator *(Vec3i self, Vec3i other) => new Vec3i(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3i operator /(Vec3i self, Vec3i other) => new Vec3i(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3i operator <<(Vec3i self, int bits) => new Vec3i(self.X << bits, self.Y << bits, self.Z << bits);
        public static Vec3i operator >>(Vec3i self, int bits) => new Vec3i(self.X >> bits, self.Y >> bits, self.Z >> bits);
        public static bool operator ==(Vec3i self, Vec3i other) => self.X == other.X && self.Y == other.Y && self.Z == other.Z;
        public static bool operator !=(Vec3i self, Vec3i other) => !(self == other);
        public int this[int index]
        {
            get 
            {
                switch (index)
                {
                case 0:
                default:
                    return X;
                case 1:
                    return Y;
                case 2:
                    return Z;
                }
            }
        }

        public int Dot(Vec3i other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public int LengthSquared() => (X * X) + (Y * Y) + (Z * Z);
        public int Length() => (int)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        public int DistanceSquared(Vec3i other) => (this - other).LengthSquared();
        public int Distance(Vec3i other) => (this - other).Length();

        public Vec3fixed ToFixed() => new Vec3fixed(new Fixed(X), new Fixed(Y), new Fixed(Z));
        public Vector3 ToFloat() => new Vector3(X, Y, Z);
        public Vec3d ToDouble() => new Vec3d(X, Y, Z);

        public override string ToString() => $"{X}, {Y}, {Z}";
        public override bool Equals(object obj) => obj is Vec3i i && X == i.X && Y == i.Y && Z == i.Z;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }

    public struct Vec3d
    {
        public double X;
        public double Y;
        public double Z;

        public Vec3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3d operator +(Vec3d self, Vec3d other) => new Vec3d(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3d operator -(Vec3d self, Vec3d other) => new Vec3d(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3d operator *(Vec3d self, Vec3d other) => new Vec3d(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3d operator *(Vec3d self, double value) => new Vec3d(self.X * value, self.Y * value, self.Z * value);
        public static Vec3d operator /(Vec3d self, Vec3d other) => new Vec3d(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3d operator /(Vec3d self, double value) => new Vec3d(self.X / value, self.Y / value, self.Z / value);
        public static bool operator !=(Vec3d self, Vec3d other) => !(self == other);

        public static bool operator ==(Vec3d self, Vec3d other)
        {
            return MathHelper.AreEqual(self.X, other.X) && MathHelper.AreEqual(self.Y, other.Y) && MathHelper.AreEqual(self.Z, other.Z);
        }

        public bool EqualTo(Vec3d other, double epsilon = 0.00001)
        {
            return MathHelper.AreEqual(X, other.X, epsilon) && MathHelper.AreEqual(Y, other.Y, epsilon) && MathHelper.AreEqual(Z, other.Z, epsilon);
        }

        public double this[int index] {
            get
            {
                switch (index)
                {
                case 0:
                default:
                    return X;
                case 1:
                    return Y;
                case 2:
                    return Z;
                }
            }
        }

        public double Dot(Vec3d other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public double LengthSquared() => (X * X) + (Y * Y) + (Z * Z);
        public double Length() => Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        public double DistanceSquared(Vec3d other) => (this - other).LengthSquared();
        public double Distance(Vec3d other) => (this - other).Length();

        public Vec3fixed ToFixed() => new Vec3fixed(new Fixed(X), new Fixed(Y), new Fixed(Z));
        public Vector3 ToFloat() => new Vector3((float)X, (float)Y, (float)Z);
        public Vec3i ToInt() => new Vec3i((int)X, (int)Y, (int)Z);

        public override string ToString() => $"{X}, {Y}, {Z}";
        public override bool Equals(object obj) => obj is Vec3d v && X == v.X && Y == v.Y && Z == v.Z;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }

    public struct Vec3fixed
    {
        public Fixed X;
        public Fixed Y;
        public Fixed Z;

        public Vec3fixed(Fixed x, Fixed y, Fixed z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3fixed operator +(Vec3fixed self, Vec3fixed other) => new Vec3fixed(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3fixed operator -(Vec3fixed self, Vec3fixed other) => new Vec3fixed(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3fixed operator *(Vec3fixed self, Vec3fixed other) => new Vec3fixed(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3fixed operator /(Vec3fixed self, Vec3fixed other) => new Vec3fixed(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static bool operator ==(Vec3fixed self, Vec3fixed other)
        {
            return MathHelper.AreEqual(self.X, other.X) && MathHelper.AreEqual(self.Y, other.Y) && MathHelper.AreEqual(self.Z, other.Z);
        }
        public static bool operator !=(Vec3fixed self, Vec3fixed other) => !(self == other);
        public double this[int index] {
            get 
            {
                switch (index)
                {
                case 0:
                default:
                    return X;
                case 1:
                    return Y;
                case 2:
                    return Z;
                }
            }
        }

        public Fixed Dot(Vec3fixed other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public Fixed LengthSquared() => (X * X) + (Y * Y) + (Z * Z);
        public Fixed Length() => new Fixed(Math.Sqrt((X * X) + (Y * Y) + (Z * Z)));
        public Fixed DistanceSquared(Vec3fixed other) => (this - other).LengthSquared();
        public Fixed Distance(Vec3fixed other) => (this - other).Length();

        public Vec3d ToDouble() => new Vec3d(X, Y, Z);
        public Vector3 ToFloat() => new Vector3(X, Y, Z);
        public Vec3i ToInt() => new Vec3i(X, Y, Z);

        public override string ToString() => $"{X}, {Y}, {Z}";
        public override bool Equals(object obj) => obj is Vec3fixed v && X == v.X && Y == v.Y && Z == v.Z;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }

    public static class Vector3Extensions
    {
        public static bool EqualTo(this Vector3 self, Vector3 other, float epsilon = 0.00001f)
        {
            return MathHelper.AreEqual(self.X, other.X, epsilon) && 
                   MathHelper.AreEqual(self.Y, other.Y, epsilon) && 
                   MathHelper.AreEqual(self.Z, other.Z, epsilon);
        }

        public static float Dot(this Vector3 vec, Vector3 other) => (vec.X * other.X) + (vec.Y * other.Y) + (vec.Z * other.Z);
        public static float LengthSquared(this Vector3 vec) => (vec.X * vec.X) + (vec.Y * vec.Y);
        public static float Length(this Vector3 vec) => (float)Math.Sqrt(LengthSquared(vec));
        public static float DistanceSquared(this Vector3 vec, Vector3 other) => (vec - other).LengthSquared();
        public static float Distance(this Vector3 vec, Vector3 other) => (vec - other).Length();

        public static Vec3i ToInt(this Vector3 vec) => new Vec3i((int)vec.X, (int)vec.Y, (int)vec.Z);
        public static Vec3fixed ToFixed(this Vector3 vec) => new Vec3fixed(new Fixed(vec.X), new Fixed(vec.Y), new Fixed(vec.Z));
        public static Vec3d ToDouble(this Vector3 vec) => new Vec3d(vec.X, vec.Y, vec.Z);
    }
}
