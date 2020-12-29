using System.Drawing;
using Helion.Util.Geometry.Boxes;

namespace Helion.Graphics.Fonts.Renderable
{
    /// <summary>
    /// A glyph to be rendered.
    /// </summary>
    public readonly struct RenderedGlyph
    {
        /// <summary>
        /// The character.
        /// </summary>
        public readonly char Character;

        /// <summary>
        /// The location in the font's atlas. This has its origin at the top
        /// left of the character.
        /// </summary>
        public readonly Rectangle Location;

        /// <summary>
        /// The UV coordinates in the font's atlas.
        /// </summary>
        public readonly Box2D UV;

        /// <summary>
        /// The color of the letter.
        /// </summary>
        public readonly Color Color;

        public RenderedGlyph(char character, Rectangle location, Box2D uv, Color color)
        {
            Character = character;
            Location = location;
            UV = uv;
            Color = color;
        }
    }
}
