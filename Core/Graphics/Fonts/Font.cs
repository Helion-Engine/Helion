using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using Helion.Geometry.Vectors;
using Helion.Graphics.Geometry;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.Fonts
{
    /// <summary>
    /// A collection of a set of glyphs for some font.
    /// </summary>
    public class Font : IFont, IEnumerable<FontGlyph>
    {
        public FontMetrics Metrics { get; }
        public Image Image { get; }
        private readonly Dictionary<char, FontGlyph> m_glyphs = new();

        public string Name { get; }
        public int MaxHeight => Image.Height;

        /// <summary>
        /// Creates a new font from a series of glyphs.
        /// </summary>
        /// <param name="name">The font name.</param>
        /// <param name="glyphs">A list of all the glyphs. This must contain
        /// the default glyph.</param>
        /// <param name="metrics">The font metrics for drawing with.</param>
        public Font(string name, IList<Glyph> glyphs, FontMetrics metrics)
        {
            Precondition(!glyphs.Empty(), "Cannot make a font that has no glyphs");

            Name = name;
            Metrics = metrics;

            ImageBox2I imageArea;
            if (glyphs.Empty())
                GenerateMissingGlyph(out imageArea);
            else
            {
                imageArea = CalculateImageArea(glyphs);
                PopulateGlyphs(glyphs, imageArea);
            }

            Image = CreateImage(imageArea);
        }

        /// <summary>
        /// Gets a glyph, or if it does not exist, returns a random glyph.
        /// </summary>
        /// <param name="c">The character to look up.</param>
        public FontGlyph this[char c] => m_glyphs.TryGetValue(c, out FontGlyph? glyph) ?
            glyph :
            m_glyphs.Values.First();
        
        public Glyph Get(char c)
        {
            // TODO: Implement when the time comes
            throw new NotImplementedException();
        }

        public bool TryGet(char c, out Glyph glyph)
        {
            // TODO: Implement when the time comes
            throw new NotImplementedException();
        }

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

        private void GenerateMissingGlyph(out ImageBox2I imageArea)
        {
            Image image = ImageHelper.CreateNullImage();
            imageArea = new ImageBox2I(0, 0, image.Width, image.Height);
            ImageBox2D uv = new(Vec2D.Zero, Vec2D.One);

            m_glyphs[IFont.DefaultChar] = new FontGlyph(IFont.DefaultChar, image, imageArea, uv);
        }

        private static ImageBox2I CalculateImageArea(IEnumerable<Glyph> glyphs)
        {
            int width = 0;
            int height = 0;

            foreach (Glyph glyph in glyphs)
            {
                width += glyph.Image.Width;
                height = Math.Max(height, glyph.Image.Height);
            }

            return new ImageBox2I(0, 0, width, height);
        }

        private void PopulateGlyphs(IEnumerable<Glyph> glyphs, ImageBox2I imageArea)
        {
            int offsetX = 0;
            Vec2D uvFactor = new Vec2D(1.0 / imageArea.Width, 1.0 / imageArea.Height);

            foreach (Glyph glyph in glyphs)
            {
                char c = glyph.Character;
                Image image = glyph.Image;

                Vec2I topLeft = new Vec2I(offsetX, 0);
                Vec2I bottomRight = new Vec2I(offsetX + image.Width, image.Height);
                ImageBox2I location = new ImageBox2I(topLeft, bottomRight);

                Vec2D topLeftUV = new Vec2D(offsetX, 0) * uvFactor;
                Vec2D bottomRightUV = new Vec2D(offsetX + image.Width, image.Height) * uvFactor;
                ImageBox2D uv = new ImageBox2D(topLeftUV, bottomRightUV);

                m_glyphs[c] = new FontGlyph(c, image, location, uv);

                offsetX += glyph.Image.Width;
            }
        }

        private Image CreateImage(ImageBox2I imageArea)
        {
            Image image = new(imageArea.Width, imageArea.Height, Color.Transparent);

            foreach (FontGlyph glyph in m_glyphs.Values)
            {
                Vec2I drawAt = new(glyph.Location.Left, glyph.Location.Top);
                glyph.Image.DrawOnTopOf(image, drawAt);
            }

            return image;
        }

        public IEnumerator<FontGlyph> GetEnumerator() => m_glyphs.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
