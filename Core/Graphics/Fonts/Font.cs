using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
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
        /// The image atlas of all the glyphs (aka: the image that contains all
        /// the compiled glyphs). They can be looked up with the character for
        /// their location in this.
        /// </summary>
        public readonly Image Atlas;

        private readonly Dictionary<char, FontGlyph> m_glyphs = new();

        /// <summary>
        /// The maximum height of any glyph.
        /// </summary>
        public int MaxHeight => Atlas.Height;

        /// <summary>
        /// Creates a new font from a series of glyphs.
        /// </summary>
        /// <param name="glyphs">A list of all the glyphs. This must contain
        /// the default glyph.</param>
        /// <param name="metrics">The font metrics for drawing with.</param>
        public Font(IList<Glyph> glyphs, FontMetrics metrics)
        {
            Precondition(!glyphs.Empty(), "Cannot make a font that has no glyphs");

            Metrics = metrics;

            Rectangle imageArea;
            if (glyphs.Empty())
                GenerateMissingGlyph(out imageArea);
            else
            {
                imageArea = CalculateImageArea(glyphs);
                PopulateGlyphs(glyphs, imageArea);
            }

            Atlas = CreateImage(imageArea);
        }

        /// <summary>
        /// Gets a glyph, or if it does not exist, returns a random glyph.
        /// </summary>
        /// <param name="c">The character to look up.</param>
        public FontGlyph this[char c] => m_glyphs.TryGetValue(c, out FontGlyph? glyph) ?
            glyph :
            m_glyphs.Values.First();

        /// <summary>
        /// Tries to get the value. See the Dictionary for analogous usage.
        /// </summary>
        /// <param name="c">The character to get.</param>
        /// <param name="glyph">The glyph, or null if no such one exists for
        /// the character.</param>
        /// <returns>True if found, false if not.</returns>
        public bool TryGetValue(char c, [NotNullWhen(true)] out FontGlyph? glyph)
        {
            return m_glyphs.TryGetValue(c, out glyph);
        }

        private void GenerateMissingGlyph(out Rectangle imageArea)
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
                Vec2D bottomLeftUv = new Vec2D(offsetX, 0) * uvFactor;
                Vec2D topRightUV = new Vec2D(offsetX + image.Width, image.Height) * uvFactor;
                Box2D uv = new Box2D(bottomLeftUv, topRightUV);

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
