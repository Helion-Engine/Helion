using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Resources;
using NLog;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Helion.Graphics.Fonts;

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
                FontFamily fontFamily = fontCollection.Add(stream);
                SixLabors.Fonts.Font imageSharpFont = fontFamily.CreateFont(RenderFontSize);
                RichTextOptions richTextOptions = new(imageSharpFont);

                string text = ComposeRenderableCharacters();
                Dictionary<char, Image> charImages = new Dictionary<char, Image>();

                foreach (char c in text)
                {
                    string charString = $"{c}";
                    // Character advance seems to be the best measurement of the pixel size required to render
                    // a character, as it measures how far over the image library would need to "move" in order to draw
                    // the next character in a string.  However, it seems like it doesn't _quite_ capture the height of
                    // characters that "dangle" under the line, like 'g'.  For now, we're adding a 4px fudge factor, but
                    // this may need to be revisited.
                    FontRectangle charAdvance = TextMeasurer.MeasureAdvance(charString, richTextOptions);

                    using (Image<Rgba32> charImage = new(
                        (int)Math.Ceiling(charAdvance.X + charAdvance.Width),
                        (int)Math.Ceiling(charAdvance.Y + charAdvance.Height + 4)))
                    {
                        charImage.Mutate(ctx =>
                        {
                            ctx.Fill(Color.Transparent.ToImageSharp);
                            ctx.DrawText(richTextOptions, charString, Color.White.ToImageSharp);
                        });

                        charImages[c] = Image.FromArgbBytes(
                            (charImage.Width, charImage.Height),
                            ExtractBytesFromRgbaImage(charImage),
                            Vec2I.Zero,
                            ResourceNamespace.Fonts)!;
                    }
                }

                var (glyphs, image) = ComposeFontGlyphs(charImages);
                return new Font(name, glyphs, image, isTrueTypeFont: true);
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

    private static byte[] ExtractBytesFromRgbaImage(Image<Rgba32> rgbaImage)
    {
        byte[] bytes = new byte[rgbaImage.Width * rgbaImage.Height * 4];
        int bytesOffset = 0;

        for (int y = 0; y < rgbaImage.Height; y++)
        {
            Span<Rgba32> pixelRow = rgbaImage.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < rgbaImage.Width; x++)
            {
                Rgba32 rgba = pixelRow[x];

                bytes[bytesOffset] = rgba.A;
                bytes[bytesOffset + 1] = rgba.R;
                bytes[bytesOffset + 2] = rgba.G;
                bytes[bytesOffset + 3] = rgba.B;

                bytesOffset += 4;
            }
        }

        return bytes;
    }

    private static (Dictionary<char, Glyph>, Image) ComposeFontGlyphs(Dictionary<char, Image> charImages)
    {
        Dictionary<char, Glyph> glyphs = new();

        int width = charImages.Values.Select(i => i.Width).Sum();
        int height = charImages.Values.Select(i => i.Height).Max();
        Image image = new(width, height, ImageType.Argb, Vec2I.Zero, ResourceNamespace.Fonts);

        int offsetX = 0;
        Vec2F totalDimension = (width, height);
        foreach ((char c, Image charImage) in charImages)
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
