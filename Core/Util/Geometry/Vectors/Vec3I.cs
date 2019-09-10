using System;
using System.Numerics;

namespace Helion.Util.Geometry.Vectors
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

        public Vec3I Abs() => new Vec3I(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
        public int Dot(Vec3I other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public int LengthSquared() => (X * X) + (Y * Y) + (Z * Z);
        public int Length() => (int)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        public int DistanceSquared(Vec3I other) => (this - other).LengthSquared();
        public int Distance(Vec3I other) => (this - other).Length();

        public Vec2I To2D() => new Vec2I(X, Y);
        public Vec3Fixed ToFixed() => new Vec3Fixed(new Fixed(X), new Fixed(Y), new Fixed(Z));
        public Vector3 ToFloat() => new Vector3(X, Y, Z);
        public Vec3D ToDouble() => new Vec3D(X, Y, Z);

        public override string ToString() => $"{X}, {Y}, {Z}";
        public override bool Equals(object? obj) => obj is Vec3I i && X == i.X && Y == i.Y && Z == i.Z;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }
}