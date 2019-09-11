using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Helion.Resources;
using Helion.Util.Extensions;
using NLog;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.Fonts.TrueTypeFont
{
    public static class TtfReader
    {
        private const int RenderFontSize = 64;
        private const char DefaultCharacter = '?';
        private const char StartCharacter = (char)32;
        private const char EndCharacter = (char)126;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// A native font reading subsystem that leverages the standard library to
        /// read fonts.
        /// </summary>
        /// <param name="data">The data to read.</param>
        /// <param name="alphaCutoff">The cutoff to which anything under it is
        /// made transparent.</param>
        /// <returns>The font, or null on failure.</returns>
        public static Font? ReadFont(byte[] data, float alphaCutoff)
        {
            // I have no idea if this can throw, or if nulls get returned (and
            // that would be an exception anyways...) so I'm playing it safe.
            try
            {
                FontCollection fontCollection = new FontCollection();
                using (MemoryStream stream = new MemoryStream(data))
                {
                    FontFamily fontFamily = fontCollection.Install(stream);
                    SixLabors.Fonts.Font imageSharpFont = fontFamily.CreateFont(RenderFontSize);
                    RendererOptions rendererOptions = new RendererOptions(imageSharpFont);

                    string text = ComposeRenderableCharacters();
                    
                    // The library isn't perfect or the lack of documentation
                    // is enough to make me confused at why I have to do extra
                    // padding, but here we go. I add 10 pixels on the width to
                    // make '~' not get cut off, and then I add a padding of 2
                    // to the top and bottom for some breathing space.
                    var (x, y, w, h) = TextMeasurer.MeasureBounds(text, rendererOptions);
                    int width = (int)(w - x + 10);
                    int height = (int)(h - y + 2 + 2);
                    PointF offset = new PointF(-x, -y + 2);
                    
                    using (Image<Rgba32> rgbaImage = new Image<Rgba32>(width, height))
                    {
                        rgbaImage.Mutate(ctx =>
                        {
                            ctx.Fill(Color.Transparent);
                            ctx.DrawText(text, imageSharpFont, Color.White, offset);
                        });
                        
                        List<Glyph> glyphs = ExtractGlyphs(rgbaImage, height, offset, rendererOptions);
                        Glyph defaultGlyph = FindDefaultGlyph(glyphs);
                        
                        FontMetrics metrics = new FontMetrics(RenderFontSize, height, 0, 0, 0);
                        return new Font(defaultGlyph, glyphs, metrics);
                    } 
                }
            }
            catch (Exception e)
            {
                Log.Error("Unable to read TTF font, unexpected error: {0}", e.Message);
                return null;
            }
        }

        private static string ComposeRenderableCharacters()
        {
            StringBuilder sb = new StringBuilder();
            for (char c = StartCharacter; c <= EndCharacter; c++)
                sb.Append(c);
            
            return sb.ToString();
        }

        private static List<Glyph> ExtractGlyphs(Image<Rgba32> rgbaImage, int height, PointF offset, 
            RendererOptions rendererOptions)
        {
            List<Glyph> glyphs = new List<Glyph>();

            for (char c = StartCharacter; c <= EndCharacter; c++)
            {
                SizeF size = TextMeasurer.Measure(c.ToString(), rendererOptions);
                int startX = (int)offset.X;
                int width = (int)size.Width;

                // This library can draw in the negative range. It sucks but it
                // is the easiest way to compensate for how the library returns
                // coordinates. The first character is a space anyways so this
                // will not be too noticeable, especially with a font size that
                // is large (like 64+).
                if (startX < 0)
                {
                    width += startX;
                    startX = 0;
                    offset.X += startX;
                    
                    // And to account for truncation (or the library), add 1... 
                    offset.X += 1;
                }
                
                // And because the library is making us do weird stuff, we have
                // to make sure we don't do anything out of bounds. This is not
                // likely triggered because we pad the glyphs, but is here for
                // safety reasons.
                if (startX + width >= rgbaImage.Width)
                    width = rgbaImage.Width - startX - 1;

                ExtractFromRgbaImage(rgbaImage, startX, width, height, out byte[] argb);

                Image image = new Image(width, height, argb, new ImageMetadata(ResourceNamespace.Fonts));
                Glyph glyph = new Glyph(c, image);
                glyphs.Add(glyph);
                
                offset.X += size.Width;
            }

            return glyphs;
        }

        private static void ExtractFromRgbaImage(Image<Rgba32> rgbaImage, int offsetX, int width, int height, 
            out byte[] bytes)
        {
            int endX = offsetX + width;
            bytes = new byte[width * height * 4];
            int bytesOffset = 0;

            for (int y = 0; y < height; y++)
            {
                Span<Rgba32> pixelRow = rgbaImage.GetPixelRowSpan(y);
                for (int x = offsetX; x < endX; x++)
                {
                    Rgba32 rgba = pixelRow[x];
                    
                    bytes[bytesOffset] = rgba.B;
                    bytes[bytesOffset + 1] = rgba.G;
                    bytes[bytesOffset + 2] = rgba.R;
                    bytes[bytesOffset + 3] = rgba.A;

                    bytesOffset += 4;
                }
            }
        }

        private static Glyph FindDefaultGlyph(IList<Glyph> glyphs)
        {
            Precondition(!glyphs.Empty(), "Should never have an empty glyph list");
            
            foreach (Glyph glyph in glyphs)
                if (glyph.Character == DefaultCharacter)
                    return glyph;

            return glyphs.First();
        }
    }
}