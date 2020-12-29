using Helion.Util.Geometry.Vectors;

namespace Helion.Graphics.Geometry
{
    /// <summary>
    /// A box in image coordinates (which has the top left as the origin, and
    /// increases down and to the right).
    /// </summary>
    public readonly struct ImageBox2D
    {
        /// <summary>
        /// A box from (0, 0) to (1, 1).
        /// </summary>
        public static readonly ImageBox2D ZeroToOne = new(Vec2D.Zero, Vec2D.One);

        /// <summary>
        /// The top left.
        /// </summary>
        public readonly Vec2D Min;

        /// <summary>
        /// The bottom right
        /// </summary>
        public readonly Vec2D Max;

        /// <summary>
        /// The left edge (X value).
        /// </summary>
        public double Left => Min.X;

        /// <summary>
        /// The top edge (Y value).
        /// </summary>
        public double Top => Min.Y;

        /// <summary>
        /// The right edge (X value).
        /// </summary>
        public double Right => Max.X;

        /// <summary>
        /// The bottom edge (Y value).
        /// </summary>
        public double Bottom => Max.Y;

        /// <summary>
        /// The width of the box.
        /// </summary>
        public double Width => Right - Left;

        /// <summary>
        /// The height of the box.
        /// </summary>
        public double Height => Bottom - Top;

        public ImageBox2D(Vec2D min, Vec2D max)
        {
            Min = min;
            Max = max;
        }

        public ImageBox2D(double startX, double startY, double endX, double endY) :
            this(new Vec2D(startX, startY), new Vec2D(endX, endY))
        {
        }

        public override string ToString() => $"({Min}, {Max})";
    }
}
