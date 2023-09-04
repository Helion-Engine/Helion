using System;
using System.Runtime.InteropServices;
using Helion.GeometryNew.Boxes;
using Helion.Util.Extensions;

namespace Helion.GeometryNew.Vectors;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Vec2i
{
    public int X;
    public int Y;

    public int Width => X;
    public int Height => Y;
    public Vec2 Float => (X, Y);
    public Vec2i Abs => (X.Abs(), Y.Abs());
    public Box2i Box => new((0, 0), (X, Y));
    public float AspectRatio => (float)Width / Height;
    public int Area => Width * Height;
    public bool HasPositiveArea => Width > 0 && Height > 0;

    public Vec2i(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static implicit operator Vec2i(ValueTuple<int, int> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    public static Vec2i operator -(Vec2i self) => new(-self.X, -self.Y);
    public static Vec2i operator +(Vec2i self, Vec2i other) => new(self.X + other.X, self.Y + other.Y);
    public static Vec2i operator -(Vec2i self, Vec2i other) => new(self.X - other.X, self.Y - other.Y);
    public static Vec2i operator *(Vec2i self, Vec2i other) => new(self.X * other.X, self.Y * other.Y);
    public static Vec2i operator *(Vec2i self, int value) => new(self.X * value, self.Y * value);
    public static Vec2i operator *(int value, Vec2i self) => new(self.X * value, self.Y * value);
    public static Vec2i operator /(Vec2i self, Vec2i other) => new(self.X / other.X, self.Y / other.Y);
    public static Vec2i operator /(Vec2i self, int value) => new(self.X / value, self.Y / value);
    public static bool operator ==(Vec2i self, Vec2i other) => self.X == other.X && self.Y == other.Y;
    public static bool operator !=(Vec2i self, Vec2i other) => !(self == other);
    
    public override string ToString() => $"{X}, {Y}";
    public override bool Equals(object? obj) => obj is Vec2i v && this == v;
    public override int GetHashCode() => HashCode.Combine(X, Y);
}