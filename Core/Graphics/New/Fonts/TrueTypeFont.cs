using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Resources;
using NLog;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace Helion.Graphics.New.Fonts
{
    public static class TrueTypeFont
    {
        private const int RenderFontSize = 64;
        private const char StartCharacter = (char)32;
        private const char EndCharacter = (char)126;
        private const int CharCount = EndCharacter - StartCharacter + 1;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        /// <summary>
        /// Reads a TTF from the data provided.
        /// </summary>
        /// <param name="name">The name of the font.</param>
        /// <param name="data">The data for the font.</param>
        /// <returns>The font, or null if it is not a TTF data set.</returns>
        public static Font? From(string name, byte[] data)
        {
            // I have no idea if this can throw, or if nulls get returned (and
            // that would be an exception anyways...) so I'm playing it safe.
            try
            {
                FontCollection fontCollection = new();
                using (MemoryStream stream = new(data))
                {
                    FontFamily fontFamily = fontCollection.Install(stream);
                    SixLabors.Fonts.Font imageSharpFont = fontFamily.CreateFont(RenderFontSize);
                    RendererOptions rendererOptions = new(imageSharpFont);

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

                    using (Image<Rgba32> rgbaImage = new(width, height))
                    {
                        rgbaImage.Mutate(ctx =>
                        {
                            ctx.Fill(Color.Transparent);
                            ctx.DrawText(text, imageSharpFont, Color.White, offset);
                        });

                        Dictionary<char, ArgbImage> charImages = ExtractGlyphs(rgbaImage, height, offset, rendererOptions);

                        var (glyphs, image) = ComposeFontGlyphs(charImages);
                        return new Font(name, glyphs, image);
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
            var chars = Enumerable.Range(StartCharacter, CharCount).Select(char.ConvertFromUtf32);
            return string.Join("", chars);
        }

        private static Dictionary<char, ArgbImage> ExtractGlyphs(Image<Rgba32> rgbaImage, int height, PointF offset,
            RendererOptions rendererOptions)
        {
            Dictionary<char, ArgbImage> glyphs = new();

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

                ArgbImage? image = ArgbImage.FromArgbBytes((width, height), argb, Vec2I.Zero, ResourceNamespace.Fonts);
                glyphs[c] = image ?? throw new Exception($"Unable to create TTF glyph character: {c}");

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
        
        private static (Dictionary<char, Glyph>, ArgbImage) ComposeFontGlyphs(Dictionary<char, ArgbImage> charImages)
        {
            Dictionary<char, Glyph> glyphs = new();

            int width = charImages.Values.Select(i => i.Width).Sum();
            int height = charImages.Values.Select(i => i.Height).Max();
            ArgbImage image = new((width, height), System.Drawing.Color.Transparent, ResourceNamespace.Fonts);

            int offsetX = 0;
            Vec2F totalDimension = (width, height);
            foreach ((char c, ArgbImage charImage) in charImages)
            {
                Vec2I start = (offsetX, 0);
                Box2I location = (start, start + (charImage.Width, charImage.Height));
                Vec2F uvStart = location.Min.Float / totalDimension;
                Vec2F uvEnd = location.Max.Float / totalDimension;
                Box2F uv = (uvStart, uvEnd);
                Glyph glyph = new(c, uv, location);
                glyphs[c] = glyph;
                
                charImage.DrawOnTopOf(image, (offsetX, 0));

                offsetX += charImage.Width;
            }

            return (glyphs, image);
        }
    }
}
