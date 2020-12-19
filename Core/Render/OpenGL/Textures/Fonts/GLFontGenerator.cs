using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Helion.Graphics;
using Helion.Graphics.Fonts;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures.Fonts
{
    /// <summary>
    /// Handles the logic of generating a font atlas that GL font textures can be
    /// made from.
    /// </summary>
    public static class GLFontGenerator
    {
        // TODO: We should grab the max texture width instead.
        private const int MaxGlyphWidth = 2048;
        private static readonly System.Drawing.Color BackgroundColor = System.Drawing.Color.Transparent;

        /// <summary>
        /// Creates a font atlas image with the metrics from a font.
        /// </summary>
        /// <param name="font">The font to create the atlas from.</param>
        /// <returns>The image and metrics that can be used by some OpenGL font
        /// implementation.</returns>
        public static (Image atlas, GLFontMetrics metrics) CreateFontAtlasFrom(Font font)
        {
            // TODO: This entire function has a potentially horrible memory footprint...
            // We add one for the default glyph. This will be a repeat, but it
            // is okay since it's just one character.
            const int glyphCount = byte.MaxValue;
            int maxWidth = Math.Max(font.Select(glyph => glyph.Image.Width).Max(), font.DefaultGlyph.Image.Width);

            // We want enough padding between it for mipmaps. Probably a bit
            // wasteful for large fonts though.
            Dimension glyphArea = new Dimension(maxWidth * 2, font.Metrics.MaxHeight * 2);
            Precondition(glyphArea.Width <= MaxGlyphWidth, "Font too large to fit onto a texture");

            int glyphsPerRow = MaxGlyphWidth / glyphArea.Width;
            int rows = (glyphCount / glyphsPerRow) + 1;
            Image image = CreateFontAtlasImage(glyphArea, glyphsPerRow, rows);

            List<GLGlyph> glyphs = CreateGlyphs(font, image, glyphArea, glyphsPerRow);
            GLFontMetrics fontMetrics = CreateFontMetrics(font, glyphs);
            return (image, fontMetrics);
        }

        private static Image CreateFontAtlasImage(Dimension glyphArea, int glyphsPerRow, int rows)
        {
            int width = glyphArea.Width * glyphsPerRow;
            int height = glyphArea.Height * rows;
            return new Image(width, height, BackgroundColor);
        }

        private static List<GLGlyph> CreateGlyphs(Font font, Image atlasImage, Dimension glyphArea, int glyphsPerRow)
        {
            List<GLGlyph> glyphs = new List<GLGlyph>();

            Vec2I drawPosition = Vec2I.Zero;
            int glyphsDrawnPerRow = 0;

            for (int asciiIndex = 0; asciiIndex < byte.MaxValue; asciiIndex++)
            {
                char c = (char)asciiIndex;
                Glyph glyph = font[c];

                glyph.Image.DrawOnTopOf(atlasImage, drawPosition);
                GLGlyph glyphData = CreateGlyphFrom(glyph, drawPosition.ToFloat(), atlasImage.Dimension);
                glyphs.Add(glyphData);

                drawPosition.X += glyphArea.Width;
                glyphsDrawnPerRow++;

                if (glyphsDrawnPerRow >= glyphsPerRow)
                {
                    drawPosition.X = 0;
                    drawPosition.Y += glyphArea.Height;
                    glyphsDrawnPerRow = 0;
                }
            }

            return glyphs;
        }

        private static GLFontMetrics CreateFontMetrics(Font font, List<GLGlyph> glyphs)
        {
            GLGlyph defaultGlyph = FindDefaultGlyph(glyphs, font.DefaultGlyph.Character);
            return new GLFontMetrics(defaultGlyph, glyphs, font.Metrics.MaxHeight);
        }

        private static GLGlyph FindDefaultGlyph(IList<GLGlyph> glyphs, char defaultChar)
        {
            Precondition(!glyphs.Empty(), "Can't find a default glyph from an empty glyph array");

            foreach (GLGlyph glyph in glyphs)
                if (glyph.Character == defaultChar)
                    return glyph;

            return glyphs.First();
        }

        private static GLGlyph CreateGlyphFrom(Glyph glyph, Vector2 position, Dimension atlasDimension)
        {
            float left = position.X / atlasDimension.Width;
            float top = position.Y / atlasDimension.Height;
            float right = (position.X + glyph.Image.Width) / atlasDimension.Width;
            float bottom = (position.Y + glyph.Image.Height) / atlasDimension.Height;
            GlyphUV uv = new GlyphUV(left, top, right, bottom);

            return new GLGlyph(glyph.Character, uv, glyph.Image.Dimension);
        }
    }
}