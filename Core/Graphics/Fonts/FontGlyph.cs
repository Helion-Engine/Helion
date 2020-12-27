using System.Drawing;
using Helion.Util.Geometry.Boxes;

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
        public readonly Rectangle Location;

        /// <summary>
        /// The UV coordinates. Intended for rendering. The top left is (0, 0)
        /// and the bottom right is (1, 1), unlike the OpenGL's convention.
        /// </summary>
        public readonly Box2D UV;

        public FontGlyph(char letter, Image image, Rectangle location, Box2D uv)
        {
            Letter = letter;
            Image = image;
            Location = location;
            UV = uv;
        }
    }
}
