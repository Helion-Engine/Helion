using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Helion.Graphics;
using Helion.Graphics.Fonts;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using MoreLinq.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Texture.Fonts
{
    public static class GLFontGenerator
    {
        private const int MaxGlyphWidth = 2048;
        private static readonly System.Drawing.Color BackgroundColor = System.Drawing.Color.Transparent;
        
        /// <summary>
        /// Creates a font atlas image with the metrics from a font.
        /// </summary>
        /// <param name="font">The font to create the atlas from.</param>
        /// <returns>The image and metrics that can be used by some OpenGL font
        /// implementation.</returns>
        public static (Image, GLFontMetrics) CreateFontAtlasFrom(Font font)
        {
            // We add one for the default glyph. This will be a repeat, but it
            // is okay since it's just one character.
            int glyphCount = font.Count() + 1;
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
            
            font.ForEach(glyph =>
            {
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
            });

            return glyphs;
        }

        private static GLFontMetrics CreateFontMetrics(Font font, List<GLGlyph> glyphs)
        {
            GLGlyph defaultGlyph = FindDefaultGlyph(glyphs, font.DefaultGlyph.Character);
            return new GLFontMetrics(defaultGlyph, glyphs);
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
            GlyphUV uv = new GlyphUV(top, left, bottom, right);
            
            return new GLGlyph(glyph.Character, uv, glyph.Image.Dimension);
        }
    }
}