using System;
using System.Numerics;

namespace Helion.Util.Geometry
{
    public struct Vec3I
    {
        public int X;
        public int Y;
        public int Z;

        public Vec3I(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3I operator +(Vec3I self, Vec3I other) => new Vec3I(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3I operator -(Vec3I self, Vec3I other) => new Vec3I(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3I operator *(Vec3I self, Vec3I other) => new Vec3I(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3I operator *(Vec3I self, int value) => new Vec3I(self.X * value, self.Y * value, self.Z * value);
        public static Vec3I operator *(int value, Vec3I self) => new Vec3I(self.X * value, self.Y * value, self.Z * value);
        public static Vec3I operator /(Vec3I self, Vec3I other) => new Vec3I(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3I operator /(Vec3I self, int value) => new Vec3I(self.X / value, self.Y / value, self.Z / value);
        public static Vec3I operator /(int value, Vec3I self) => new Vec3I(self.X / value, self.Y / value, self.Z / value);
        public static Vec3I operator <<(Vec3I self, int bits) => new Vec3I(self.X << bits, self.Y << bits, self.Z << bits);
        public static Vec3I operator >>(Vec3I self, int bits) => new Vec3I(self.X >> bits, self.Y >> bits, self.Z >> bits);
        public static bool operator ==(Vec3I self, Vec3I other) => self.X == other.X && self.Y == other.Y && self.Z == other.Z;
        public static bool operator !=(Vec3I self, Vec3I other) => !(self == other);
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

        public int Dot(Vec3I other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public int LengthSquared() => (X * X) + (Y * Y) + (Z * Z);
        public int Length() => (int)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        public int DistanceSquared(Vec3I other) => (this - other).LengthSquared();
        public int Distance(Vec3I other) => (this - other).Length();

        public Vec3Fixed ToFixed() => new Vec3Fixed(new Fixed(X), new Fixed(Y), new Fixed(Z));
        public Vector3 ToFloat() => new Vector3(X, Y, Z);
        public Vec3D ToDouble() => new Vec3D(X, Y, Z);

        public override string ToString() => $"{X}, {Y}, {Z}";
        public override bool Equals(object obj) => obj is Vec3I i && X == i.X && Y == i.Y && Z == i.Z;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }

    public struct Vec3D
    {
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

        public bool EqualTo(Vec3D other, double epsilon = 0.00001)
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

        public Vec3D Unit() => this / Length();
        public void Normalize() => this /= Length();
        public double Dot(Vec3D other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public double LengthSquared() => (X * X) + (Y * Y) + (Z * Z);
        public double Length() => Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        public double DistanceSquared(Vec3D other) => (this - other).LengthSquared();
        public double Distance(Vec3D other) => (this - other).Length();

        public Vec3Fixed ToFixed() => new Vec3Fixed(new Fixed(X), new Fixed(Y), new Fixed(Z));
        public Vector3 ToFloat() => new Vector3((float)X, (float)Y, (float)Z);
        public Vec3I ToInt() => new Vec3I((int)X, (int)Y, (int)Z);

        public override string ToString() => $"{X}, {Y}, {Z}";
        public override bool Equals(object obj) => obj is Vec3D v && X == v.X && Y == v.Y && Z == v.Z;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }

    public struct Vec3Fixed
    {
        public Fixed X;
        public Fixed Y;
        public Fixed Z;

        public Vec3Fixed(Fixed x, Fixed y, Fixed z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3Fixed operator +(Vec3Fixed self, Vec3Fixed other) => new Vec3Fixed(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3Fixed operator -(Vec3Fixed self, Vec3Fixed other) => new Vec3Fixed(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3Fixed operator *(Vec3Fixed self, Vec3Fixed other) => new Vec3Fixed(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3Fixed operator *(Vec3Fixed self, Fixed value) => new Vec3Fixed(self.X * value, self.Y * value, self.Z * value);
        public static Vec3Fixed operator *(Fixed value, Vec3Fixed self) => new Vec3Fixed(self.X * value, self.Y * value, self.Z * value);
        public static Vec3Fixed operator /(Vec3Fixed self, Vec3Fixed other) => new Vec3Fixed(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3Fixed operator /(Vec3Fixed self, Fixed value) => new Vec3Fixed(self.X / value, self.Y / value, self.Z / value);
        public static Vec3Fixed operator /(Fixed value, Vec3Fixed self) => new Vec3Fixed(self.X / value, self.Y / value, self.Z / value);
        public static bool operator ==(Vec3Fixed self, Vec3Fixed other)
        {
            return MathHelper.AreEqual(self.X, other.X) && MathHelper.AreEqual(self.Y, other.Y) && MathHelper.AreEqual(self.Z, other.Z);
        }
        public static bool operator !=(Vec3Fixed self, Vec3Fixed other) => !(self == other);
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

        public Vec3Fixed Unit() => this / Length();
        public void Normalize() => this /= Length();
        public Fixed Dot(Vec3Fixed other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public Fixed LengthSquared() => (X * X) + (Y * Y) + (Z * Z);
        public Fixed Length() => new Fixed(Math.Sqrt((X * X) + (Y * Y) + (Z * Z)));
        public Fixed DistanceSquared(Vec3Fixed other) => (this - other).LengthSquared();
        public Fixed Distance(Vec3Fixed other) => (this - other).Length();

        public Vec3D ToDouble() => new Vec3D(X, Y, Z);
        public Vector3 ToFloat() => new Vector3(X, Y, Z);
        public Vec3I ToInt() => new Vec3I(X, Y, Z);

        public override string ToString() => $"{X}, {Y}, {Z}";
        public override bool Equals(object obj) => obj is Vec3Fixed v && X == v.X && Y == v.Y && Z == v.Z;
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

        public static Vector3 Unit(this Vector3 vec) => vec / vec.Length();
        public static Vec3I ToInt(this Vector3 vec) => new Vec3I((int)vec.X, (int)vec.Y, (int)vec.Z);
        public static Vec3Fixed ToFixed(this Vector3 vec) => new Vec3Fixed(new Fixed(vec.X), new Fixed(vec.Y), new Fixed(vec.Z));
        public static Vec3D ToDouble(this Vector3 vec) => new Vec3D(vec.X, vec.Y, vec.Z);
    }
}
