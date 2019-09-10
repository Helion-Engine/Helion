using System;
using System.Numerics;

namespace Helion.Util.Geometry.Vectors
{
    public struct Vec3D
    {
        public static readonly Vec3D Zero = new Vec3D(0, 0, 0);
        
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

        public double this[int index] 
        {
            get
            {
                switch (index)
                {
                default:
                    return X;
                case 1:
                    return Y;
                case 2:
                    return Z;
                }
            }
        }

        public Vec3D Abs() => new Vec3D(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
        public Vec3D Unit() => this / Length();
        public void Normalize() => this /= Length();
        public double Dot(Vec3D other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public double LengthSquared() => (X * X) + (Y * Y) + (Z * Z);
        public double Length() => Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        public double DistanceSquared(Vec3D other) => (this - other).LengthSquared();
        public double Distance(Vec3D other) => (this - other).Length();
        public Vec3D Interpolate(Vec3D end, double t) => this + (t * (end - this));

        public Vec2D To2D() => new Vec2D(X, Y);
        public Vec3Fixed ToFixed() => new Vec3Fixed(new Fixed(X), new Fixed(Y), new Fixed(Z));
        public Vector3 ToFloat() => new Vector3((float)X, (float)Y, (float)Z);
        public Vec3I ToInt() => new Vec3I((int)X, (int)Y, (int)Z);

        public override string ToString() => $"{X}, {Y}, {Z}";
        public override bool Equals(object? obj) => obj is Vec3D v && X == v.X && Y == v.Y && Z == v.Z;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }
}