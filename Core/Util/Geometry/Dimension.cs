﻿using System;
using System.Diagnostics.Contracts;
using Helion.Util.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Geometry
{
    /// <summary>
    /// A simple dimension wrapper around a width and height.
    /// </summary>
    public struct Dimension
    {
        public int Width;
        public int Height;
        
        public float AspectRatio => ((float)Width) / Height;
        public Vec2I Vector => new(Width, Height);

        public Dimension(int width, int height)
        {
            Precondition(width >= 0, "Dimension width should not be negative");
            Precondition(height >= 0, "Dimension height should not be negative");

            Width = width;
            Height = height;
        }

        public static bool operator==(Dimension first, Dimension second)
        {
            return first.Width == second.Width && first.Height == second.Height;
        }

        public static bool operator!=(Dimension first, Dimension second)
        {
            return !(first == second);
        }

        public void Deconstruct(out int width, out int height)
        {
            width = Width;
            height = Height;
        }

        public void Scale(float scale)
        {
            Width = (int)(Width * scale);
            Height = (int)(Height * scale);
        }

        public bool Equals(Dimension other) => Width == other.Width && Height == other.Height;

        public override bool Equals(object? obj) => obj is Dimension other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Width, Height);
        public override string ToString() => $"{Width}, {Height}";
    }
}
