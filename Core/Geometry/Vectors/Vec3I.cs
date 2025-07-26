// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Helion.Geometry.Boxes;
using Helion.Util.Configs.Impl;
using Helion.Util.Extensions;

namespace Helion.Geometry.Vectors
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vec3I(int x, int y, int z)
    {
        public static readonly Vec3I Zero = new(0, 0, 0);
        public static readonly Vec3I One = new(1, 1, 1);

        public int X = x;
        public int Y = y;
        public int Z = z;

        public readonly Vec2I XY => new(X, Y);
        public readonly Vec2I XZ => new(X, Z);
        public readonly Vec3F Float => new((float)X, (float)Y, (float)Z);
        public readonly Vec3D Double => new((double)X, (double)Y, (double)Z);
        public readonly Vec3Fixed FixedPoint => new(Fixed.From(X), Fixed.From(Y), Fixed.From(Z));
        public readonly Box3I Box => new((0, 0, 0), (X, Y, Z));

        public static implicit operator Vec3I(ValueTuple<int, int, int> tuple)
        {
            return new(tuple.Item1, tuple.Item2, tuple.Item3);
        }
               
        public static Vec3I operator -(Vec3I self) => new(-self.X, -self.Y, -self.Z);
        public static Vec3I operator +(Vec3I self, Vec3I other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3I operator +(Vec3I self, Vector3I other) => new(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        public static Vec3I operator -(Vec3I self, Vec3I other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3I operator -(Vec3I self, Vector3I other) => new(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        public static Vec3I operator *(Vec3I self, Vec3I other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3I operator *(Vec3I self, Vector3I other) => new(self.X * other.X, self.Y * other.Y, self.Z * other.Z);
        public static Vec3I operator *(Vec3I self, int value) => new(self.X * value, self.Y * value, self.Z * value);
        public static Vec3I operator *(int value, Vec3I self) => new(self.X * value, self.Y * value, self.Z * value);
        public static Vec3I operator /(Vec3I self, Vec3I other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3I operator /(Vec3I self, Vector3I other) => new(self.X / other.X, self.Y / other.Y, self.Z / other.Z);
        public static Vec3I operator /(Vec3I self, int value) => new(self.X / value, self.Y / value, self.Z / value);
        public static bool operator ==(Vec3I self, Vec3I other) => self.X == other.X && self.Y == other.Y && self.Z == other.Z;
        public static bool operator !=(Vec3I self, Vec3I other) => !(self == other);

        public readonly Vec3I WithX(int x) => new(x, Y, Z);
        public readonly Vec3I WithY(int y) => new(X, y, Z);
        public readonly Vec3I WithZ(int z) => new(X, Y, z);
        public readonly Vec4I To4D(int w) => new(X, Y, Z, w);

        public readonly Vec3I Abs() => new(X.Abs(), Y.Abs(), Z.Abs());
        public readonly int Dot(Vec3I other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
        public readonly int Dot(Vector3I other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);

        public override readonly string ToString() => $"{X}, {Y}, {Z}";
        public override readonly bool Equals(object? obj) => obj is Vec3I v && X == v.X && Y == v.Y && Z == v.Z;
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);

        public static Vec3I FromConfigString(string s)
        {
            try
            {
                var tokens = s.Split(Config.FindSplitValue(s));
                var x = int.Parse(tokens[0].Trim(), CultureInfo.InvariantCulture);
                var y = int.Parse(tokens[1].Trim(), CultureInfo.InvariantCulture);
                var z = int.Parse(tokens[2].Trim(), CultureInfo.InvariantCulture);
                return (x, y, z);
            }
            catch
            {
                return (1, 1, 1);
            }
        }
    }
}
