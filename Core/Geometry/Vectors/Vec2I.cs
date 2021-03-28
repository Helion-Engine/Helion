// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using GlmSharp;
using Helion.Util.Extensions;

namespace Helion.Geometry.Vectors
{
    public struct Vec2I
    {
        public static readonly Vec2I Zero = (0, 0);
        public static readonly Vec2I One = (1, 1);

        public int X;
        public int Y;

        public int U => X;
        public int V => Y;
        public Vec2F Float => new((float)X, (float)Y);
        public Vec2D Double => new((double)X, (double)Y);
        public Vec2Fixed FixedPoint => new(Fixed.From(X), Fixed.From(Y));
        public IEnumerable<int> Values => GetEnumerableValues();

        public Vec2I(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator Vec2I(ValueTuple<int, int> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public void Deconstruct(out int x, out int y)
        {
            x = X;
            y = Y;
        }

        public int this[int index]
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

        public static Vec2I operator -(Vec2I self) => new(-self.X, -self.Y);
        public static Vec2I operator +(Vec2I self, Vec2I other) => new(self.X + other.X, self.Y + other.Y);
        public static Vec2I operator +(Vec2I self, Vector2I other) => new(self.X + other.X, self.Y + other.Y);
        public static Vec2I operator -(Vec2I self, Vec2I other) => new(self.X - other.X, self.Y - other.Y);
        public static Vec2I operator -(Vec2I self, Vector2I other) => new(self.X - other.X, self.Y - other.Y);
        public static Vec2I operator *(Vec2I self, Vec2I other) => new(self.X * other.X, self.Y * other.Y);
        public static Vec2I operator *(Vec2I self, Vector2I other) => new(self.X * other.X, self.Y * other.Y);
        public static Vec2I operator *(Vec2I self, int value) => new(self.X * value, self.Y * value);
        public static Vec2I operator *(int value, Vec2I self) => new(self.X * value, self.Y * value);
        public static Vec2I operator /(Vec2I self, Vec2I other) => new(self.X / other.X, self.Y / other.Y);
        public static Vec2I operator /(Vec2I self, Vector2I other) => new(self.X / other.X, self.Y / other.Y);
        public static Vec2I operator /(Vec2I self, int value) => new(self.X / value, self.Y / value);
        public static bool operator ==(Vec2I self, Vec2I other) => self.X == other.X && self.Y == other.Y;
        public static bool operator ==(Vec2I self, Vector2I other) => self.X == other.X && self.Y == other.Y;
        public static bool operator !=(Vec2I self, Vec2I other) => !(self == other);
        public static bool operator !=(Vec2I self, Vector2I other) => !(self == other);

        public Vec2I WithX(int x) => new(x, Y);
        public Vec2I WithY(int y) => new(X, y);
        public Vec3I To3D(int z) => new(X, Y, z);

        public Vec2I Abs() => new(X.Abs(), Y.Abs());
        public int Dot(Vec2I other) => (X * other.X) + (Y * other.Y);
        public int Dot(Vector2I other) => (X * other.X) + (Y * other.Y);

        private IEnumerable<int> GetEnumerableValues()
        {
            yield return X;
            yield return Y;
        }

        public override string ToString() => $"{X}, {Y}";
        public override bool Equals(object? obj) => obj is Vec2I v && X == v.X && Y == v.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }
}
