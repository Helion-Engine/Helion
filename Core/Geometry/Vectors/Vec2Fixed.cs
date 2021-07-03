// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using GlmSharp;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Util.Extensions;

namespace Helion.Geometry.Vectors
{
    public struct Vec2Fixed
    {
        public static readonly Vec2Fixed Zero = new(Fixed.Zero(), Fixed.Zero());
        public static readonly Vec2Fixed One = new(Fixed.One(), Fixed.One());

        public Fixed X;
        public Fixed Y;

        public Fixed U => X;
        public Fixed V => Y;
        public Vec2I Int => new(X.ToInt(), Y.ToInt());
        public Vec2F Float => new(X.ToFloat(), Y.ToFloat());
        public Vec2D Double => new(X.ToDouble(), Y.ToDouble());
        public IEnumerable<Fixed> Values => GetEnumerableValues();

        public Vec2Fixed(Fixed x, Fixed y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator Vec2Fixed(ValueTuple<Fixed, Fixed> tuple)
        {
            return new(tuple.Item1, tuple.Item2);
        }

        public void Deconstruct(out Fixed x, out Fixed y)
        {
            x = X;
            y = Y;
        }

        public Fixed this[int index]
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

        public static Vec2Fixed operator -(Vec2Fixed self) => new(-self.X, -self.Y);
        public static Vec2Fixed operator +(Vec2Fixed self, Vec2Fixed other) => new(self.X + other.X, self.Y + other.Y);
        public static Vec2Fixed operator +(Vec2Fixed self, Vector2Fixed other) => new(self.X + other.X, self.Y + other.Y);
        public static Vec2Fixed operator -(Vec2Fixed self, Vec2Fixed other) => new(self.X - other.X, self.Y - other.Y);
        public static Vec2Fixed operator -(Vec2Fixed self, Vector2Fixed other) => new(self.X - other.X, self.Y - other.Y);
        public static Vec2Fixed operator *(Vec2Fixed self, Vec2Fixed other) => new(self.X * other.X, self.Y * other.Y);
        public static Vec2Fixed operator *(Vec2Fixed self, Vector2Fixed other) => new(self.X * other.X, self.Y * other.Y);
        public static Vec2Fixed operator *(Vec2Fixed self, Fixed value) => new(self.X * value, self.Y * value);
        public static Vec2Fixed operator *(Fixed value, Vec2Fixed self) => new(self.X * value, self.Y * value);
        public static Vec2Fixed operator /(Vec2Fixed self, Vec2Fixed other) => new(self.X / other.X, self.Y / other.Y);
        public static Vec2Fixed operator /(Vec2Fixed self, Vector2Fixed other) => new(self.X / other.X, self.Y / other.Y);
        public static Vec2Fixed operator /(Vec2Fixed self, Fixed value) => new(self.X / value, self.Y / value);
        public static bool operator ==(Vec2Fixed self, Vec2Fixed other) => self.X == other.X && self.Y == other.Y;
        public static bool operator !=(Vec2Fixed self, Vec2Fixed other) => !(self == other);

        public Vec2Fixed WithX(Fixed x) => new(x, Y);
        public Vec2Fixed WithY(Fixed y) => new(X, y);
        public Vec3Fixed To3D(Fixed z) => new(X, Y, z);

        public Vec2Fixed Abs() => new(X.Abs(), Y.Abs());
        public Fixed Dot(Vec2Fixed other) => (X * other.X) + (Y * other.Y);
        public Fixed Dot(Vector2Fixed other) => (X * other.X) + (Y * other.Y);

        private IEnumerable<Fixed> GetEnumerableValues()
        {
            yield return X;
            yield return Y;
        }

        public override string ToString() => $"{X}, {Y}";
        public override bool Equals(object? obj) => obj is Vec2Fixed v && X == v.X && Y == v.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }
}
