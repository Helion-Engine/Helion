using System;
using Helion.Util.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Geometry
{
    /// <summary>
    /// A simple dimension wrapper around a width and height.
    /// </summary>
    public struct Dimension
    {
        /// <summary>
        /// The width of the dimension.
        /// </summary>
        public int Width;

        /// <summary>
        /// The height of the dimension.
        /// </summary>
        public int Height;

        /// <summary>
        /// Creates a new dimension object with the dimensions provided.
        /// </summary>
        /// <param name="width">The width which should be >= 0.</param>
        /// <param name="height">The height which should be >= 0.</param>
        public Dimension(int width, int height)
        {
            Precondition(width >= 0, "Dimension width must not be negative");
            Precondition(height >= 0, "Dimension height must not be negative");

            Width = width;
            Height = height;
        }

        /// <summary>
        /// Deconstructs the dimension into its components.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public void Deconstruct(out int width, out int height)
        {
            width = Width;
            height = Height;
        }

        /// <summary>
        /// Checks for equality between dimensions.
        /// </summary>
        /// <param name="other">The other dimension.</param>
        /// <returns>True if they have the same width and height, false if not.
        /// </returns>
        public bool Equals(Dimension other) => Width == other.Width && Height == other.Height;

        /// <summary>
        /// Calculates the aspect ratio of width by height.
        /// </summary>
        public float AspectRatio => ((float)Width) / Height;

        /// <summary>
        /// Gets the value as a vector.
        /// </summary>
        /// <returns>The vector representation of this object.</returns>
        public Vec2I ToVector() => new Vec2I(Width, Height);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is Dimension other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Width, Height);

        /// <inheritdoc/>
        public override string ToString() => $"{Width}, {Height}";
    }
}
