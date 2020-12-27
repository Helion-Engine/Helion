using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.Fonts
{
    /// <summary>
    /// A collection of a set of glyphs for some font.
    /// </summary>
    public class Font : IEnumerable<FontGlyph>
    {
        /// <summary>
        /// The character to use when a letter cannot be found.
        /// </summary>
        public const char DefaultChar = '?';

        /// <summary>
        /// The metrics for the entire font.
        /// </summary>
        public readonly FontMetrics Metrics;

        /// <summary>
        /// The image that contains all the compiled glyphs. They can be looked
        /// up with the character glyph for their location in this.
        /// </summary>
        public readonly Image Image;

        private readonly Dictionary<char, FontGlyph> m_glyphs = new();

        /// <summary>
        /// Creates a new font from a series of glyphs.
        /// </summary>
        /// <param name="glyphs">A list of all the glyphs. This must contain
        /// the default glyph.</param>
        /// <param name="metrics">The font metrics for drawing with.</param>
        public Font(List<Glyph> glyphs, FontMetrics metrics)
        {
            Precondition(!glyphs.Empty(), "Cannot make a font that has no glyphs");

            Metrics = metrics;

            Rectangle imageArea = default;
            if (glyphs.Empty())
                GenerateMissingGlyph(ref imageArea);
            else
            {
                imageArea = CalculateImageArea(glyphs);
                PopulateGlyphs(glyphs, imageArea);
            }

            Image = CreateImage(imageArea);
        }

        private void GenerateMissingGlyph(ref Rectangle imageArea)
        {
            Image image = ImageHelper.CreateNullImage();
            imageArea = new Rectangle(0, 0, image.Width, image.Height);
            Box2D uv = new(Vec2D.Zero, Vec2D.One);

            m_glyphs[DefaultChar] = new FontGlyph(DefaultChar, image, imageArea, uv);
        }

        private static Rectangle CalculateImageArea(IEnumerable<Glyph> glyphs)
        {
            int width = 0;
            int height = 0;

            foreach (Glyph glyph in glyphs)
            {
                width += glyph.Image.Width;
                height = Math.Max(height, glyph.Image.Height);
            }

            return new Rectangle(0, 0, width, height);
        }

        private void PopulateGlyphs(IEnumerable<Glyph> glyphs, Rectangle imageArea)
        {
            int offsetX = 0;
            Vec2D uvFactor = new Vec2D(1.0 / imageArea.Width, 1.0 / imageArea.Height);

            foreach (Glyph glyph in glyphs)
            {
                char c = glyph.Character;
                Image image = glyph.Image;

                Rectangle location = new Rectangle(offsetX, 0, image.Width, image.Height);
                Vec2D topLeftUV = new Vec2D(offsetX, 0) * uvFactor;
                Vec2D bottomRightUV = new Vec2D(offsetX + image.Width, image.Height) * uvFactor;
                Box2D uv = new Box2D(topLeftUV, bottomRightUV);

                m_glyphs[c] = new FontGlyph(c, image, location, uv);

                offsetX += glyph.Image.Width;
            }
        }

        private Image CreateImage(Rectangle imageArea)
        {
            Image image = new(imageArea.Width, imageArea.Height, Color.Transparent);

            foreach (FontGlyph glyph in m_glyphs.Values)
            {
                Vec2I drawAt = new(glyph.Location.X, glyph.Location.Y);
                glyph.Image.DrawOnTopOf(image, drawAt);
            }

            return image;
        }

        public IEnumerator<FontGlyph> GetEnumerator() => m_glyphs.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
