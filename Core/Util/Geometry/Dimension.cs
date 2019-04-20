namespace Helion.Util.Geometry
{
    /// <summary>
    /// A simple dimension wrapper around a width and height.
    /// </summary>
    public struct Dimension
    {
        public int Width;
        public int Height;

        public Dimension(int width, int height)
        {
            Assert.Precondition(width >= 0, "Dimension width must not be negative");
            Assert.Precondition(height >= 0, "Dimension height must not be negative");

            Width = width;
            Height = height;
        }

        /// <summary>
        /// Calculates the aspect ratio of width by height.
        /// </summary>
        public float AspectRatio => ((float)Width) / Height;

        public override string ToString() => $"{Width}, {Height}";
    }
}
