using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;

namespace Helion.Graphics.Geometry
{
    /// <summary>
    /// A box in image coordinates (which has the top left as the origin, and
    /// increases down and to the right).
    /// </summary>
    public readonly struct ImageBox2I
    {
        /// <summary>
        /// The top left.
        /// </summary>
        public readonly Vec2I Min;

        /// <summary>
        /// The bottom right
        /// </summary>
        public readonly Vec2I Max;

        /// <summary>
        /// The left edge (X value).
        /// </summary>
        public int Left => Min.X;

        /// <summary>
        /// The top edge (Y value).
        /// </summary>
        public int Top => Min.Y;

        /// <summary>
        /// The right edge (X value).
        /// </summary>
        public int Right => Max.X;

        /// <summary>
        /// The bottom edge (Y value).
        /// </summary>
        public int Bottom => Max.Y;

        /// <summary>
        /// The width of the box.
        /// </summary>
        public int Width => Right - Left;

        /// <summary>
        /// The height of the box.
        /// </summary>
        public int Height => Bottom - Top;

        public ImageBox2I(Vec2I min, Vec2I max)
        {
            Min = min;
            Max = max;
        }

        public ImageBox2I(int startX, int startY, int endX, int endY) :
            this(new Vec2I(startX, startY), new Vec2I(endX, endY))
        {
        }

        public Dimension ToDimension() => new(Width, Height);

        public override string ToString() => $"({Min}, {Max})";
    }
}