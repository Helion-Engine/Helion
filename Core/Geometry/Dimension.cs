using System;
using Helion.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Geometry;

/// <summary>
/// A simple dimension wrapper around a width and height.
/// </summary>
public struct Dimension
{
    public int Width;
    public int Height;

    public float AspectRatio => (float)Width / Height;
    public Vec2I Vector => new(Width, Height);
    public int Area => Width * Height;

    public Dimension(Vec2I dimensions) : this(dimensions.X, dimensions.Y)
    {
    }

    public Dimension(int width, int height)
    {
        Precondition(width >= 0, "Dimension width should not be negative");
        Precondition(height >= 0, "Dimension height should not be negative");

        Width = width;
        Height = height;
    }

    public static implicit operator Dimension(ValueTuple<int, int> tuple)
    {
        return new(tuple.Item1, tuple.Item2);
    }

    public void Deconstruct(out int width, out int height)
    {
        width = Width;
        height = Height;
    }

    public static bool operator==(Dimension first, Dimension second)
    {
        return first.Width == second.Width && first.Height == second.Height;
    }

    public static bool operator!=(Dimension first, Dimension second)
    {
        return !(first == second);
    }

    public static Vec2I operator+(Vec2I vec, Dimension dim)
    {
        return (vec.X + dim.Width, vec.Y + dim.Height);
    }

    public void Scale(float scale)
    {
        Width = (int)(Width * scale);
        Height = (int)(Height * scale);
    }

    public bool Equals(Dimension other) => Width == other.Width && Height == other.Height;

    public static Dimension FromConfigString(string s)
    {
        try
        {
            var tokens = s.Split(",");
            int w = int.Parse(tokens[0].Trim());
            int h = int.Parse(tokens[1].Trim());
            return (w, h);
        }
        catch
        {
            return (1, 1);
        }
    }

    public override bool Equals(object? obj) => obj is Dimension other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Width, Height);
    public override string ToString() => $"{Width}, {Height}";
}
