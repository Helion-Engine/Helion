using Helion.Graphics.Geometry;

namespace Helion.Graphics.Fonts
{
    /// <summary>
    /// A glyph for a font. This is the atomic element in a font.
    /// </summary>
    public record FontGlyph
    {
        /// <summary>
        /// The letter this glyph represents.
        /// </summary>
        public readonly char Letter;

        /// <summary>
        /// The image that makes up this glyph.
        /// </summary>
        public readonly Image Image;

        /// <summary>
        /// The location in the larger image of a font. Used primarily for
        /// rendering. The origin is at the top left of the image.
        /// </summary>
        public readonly ImageBox2I Location;

        /// <summary>
        /// The UV coordinates. Intended for rendering. Bottom left is (0, 0)
        /// and the top right is (1, 1), as per OpenGL conventions.
        /// </summary>
        public readonly ImageBox2D UV;

        public FontGlyph(char letter, Image image, ImageBox2I location, ImageBox2D uv)
        {
            Letter = letter;
            Image = image;
            Location = location;
            UV = uv;
        }
    }
}
