using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics.Fonts;
using Helion.Graphics.Geometry;
using Helion.Render.Legacy.Commands.Alignment;
using Helion.Util;
using Helion.Util.Extensions;

namespace Helion.Render.Legacy.Texture.Fonts;

/// <summary>
/// A collection of render information that can be used to draw a string.
/// </summary>
public class RenderableString
{
    public static readonly Color DefaultColor = Color.White;

    private static readonly List<ColorRange> ColorRanges = new();

    /// <summary>
    /// The font used when rendering this.
    /// </summary>
    public Graphics.Fonts.Font Font;

    /// <summary>
    /// The area that encapsulates all the glyphs.
    /// </summary>
    public Dimension DrawArea;

    /// <summary>
    /// All the glyphs and their positions to be drawn.
    /// </summary>
    public List<RenderableSentence> Sentences;

    /// <summary>
    /// Creates a rendered string that is ready to be passed to a renderer.
    /// </summary>
    /// <param name="dataCache">The DataCache to use.</param>
    /// <param name="font">The font to use.</param>
    /// <param name="str">The colored string to process.</param>
    /// <param name="fontSize">The height of the characters, in pixels. If
    /// the font has space padding on the top and bottom, those are taken
    /// into account. If you do not want that, you have to trim your fonts.
    /// </param>
    /// <param name="align">Alignment (only needed if there are multiple
    /// lines, otherwise it does not matter).</param>
    /// <param name="maxWidth">How wide before wrapping around.</param>
    public RenderableString(DataCache dataCache, string str, Graphics.Fonts.Font font, int fontSize, TextAlign align = TextAlign.Left,
        int maxWidth = int.MaxValue)
    {
        Font = font;
        Sentences = PopulateSentences(dataCache, str, font, fontSize, maxWidth);
        DrawArea = CalculateDrawArea(Sentences);
        AlignTo(align);
        RecalculateGlyphLocations();
    }

    public void Set(DataCache dataCache, string str, Graphics.Fonts.Font font, int fontSize, TextAlign align = TextAlign.Left,
        int maxWidth = int.MaxValue)
    {
        Font = font;
        Sentences = PopulateSentences(dataCache, str, font, fontSize, maxWidth);
        DrawArea = CalculateDrawArea(Sentences);
        AlignTo(align);
        RecalculateGlyphLocations();
    }

    public static List<RenderableSentence> PopulateSentences(DataCache dataCache, string str, Graphics.Fonts.Font font, int fontSize,
        int maxWidth)
    {
        double scale = (double)fontSize / font.MaxHeight;
        int currentWidth = 0;
        int currentHeight = 0;

        List<RenderableSentence> sentences = dataCache.GetRenderableSentences();
        if (string.IsNullOrEmpty(str))
            return sentences;

        List<RenderableGlyph>? currentSentence = null;
        var colorRanges = GetColorRanges(str);
        foreach (var colorRange in colorRanges)
        {
            for (int i = colorRange.StartIndex; i < colorRange.EndIndex; i++)
            {
                char c = str[i];
                Glyph glyph = font.Get(c);
                (int glyphW, int glyphH) = glyph.Area.Dimension;

                int endX = currentWidth + (int)(glyphW * scale);
                int endY = currentHeight + (int)(glyphH * scale);

                // We want to make sure each sentence has one character to avoid infinite looping cases where width is too small.
                if (endX > maxWidth && currentSentence != null && currentSentence.Count > 0)
                {
                    CreateAndAddSentenceIfPossible();
                    continue;
                }

                // We use a dummy box temporarily, and calculate it at the end properly (for code clarity reasons).
                ImageBox2I drawLoc = new(currentWidth, currentHeight, endX, endY);
                ImageBox2D uv = new(glyph.UV.Min.Double, glyph.UV.Max.Double);

                RenderableGlyph renderableGlyph = new(c, drawLoc, ImageBox2D.ZeroToOne, uv, colorRange.Color);
                if (currentSentence == null)
                    currentSentence = dataCache.GetRenderableGlyphs();
                currentSentence.Add(renderableGlyph);

                currentWidth = endX;
            }
        }

        CreateAndAddSentenceIfPossible();
        return sentences;

        void CreateAndAddSentenceIfPossible()
        {
            if (currentSentence == null || currentSentence.Count == 0)
                return;

            RenderableSentence sentence = new(currentSentence);
            sentences.Add(sentence);
            currentSentence = null;

            currentWidth = 0;
            currentHeight += sentence.DrawArea.Height;
        }
    }

    private static List<ColorRange> GetColorRanges(string str)
    {
        ColorRanges.Clear();
        ColorRanges.Add(new ColorRange(0, DefaultColor));

        bool success = FindNextColorIndex(str, 0, out int startIndex, out int endIndex);
        while (success)
        {
            ColorRange currentColorInfo = ColorRanges.Last();
            currentColorInfo.EndIndex = startIndex;
            ColorRanges[^1] = currentColorInfo;

            Color color = ColorDefinitionToColor(str.AsSpan(startIndex, endIndex - startIndex));
            ColorRanges.Add(new ColorRange(endIndex, color));
            startIndex = endIndex + 1;
            success = FindNextColorIndex(str, startIndex, out startIndex, out endIndex);
        }

        // Since we never set the very last element's ending point due to
        // the loop invariant, we do that now.
        var last = ColorRanges.Last();
        last.EndIndex = str.Length;
        ColorRanges[^1] = last;

        if (last.StartIndex == last.EndIndex)
            ColorRanges.RemoveAt(ColorRanges.Count - 1);

        return ColorRanges;
    }

    private static bool FindNextColorIndex(string str, int index, out int startIndex, out int endIndex)
    {
        //\c[1,2,3]
        startIndex = -1;
        endIndex = -1;
        while (index < str.Length)
        {
            while (index < str.Length && str[index++] != '\\') ;

            startIndex = index - 1;

            while (index < str.Length && str[index++] != 'c') ;

            if (index >= str.Length || str[index++] != '[')
                continue;

            for (int i = 0; i < 3; i++)
            {
                int scanIndex = index;
                while (index < str.Length && char.IsDigit(str[index]) && index - scanIndex <= 3)
                    index++;

                if (i < 2 && str[index++] != ',')
                    continue;
            }

            if (index >= str.Length || str[index++] != ']')
                continue;

            endIndex = index;
            return true;
        }

        return false;
    }

    private static Color ColorDefinitionToColor(ReadOnlySpan<char> rgbColorCode)
    {
        int startIndex = 3;
        int colorIndex = 0;
        int r = DefaultColor.R;
        int g = DefaultColor.G;
        int b = DefaultColor.B;

        for (int i = startIndex; i < rgbColorCode.Length; i++)
        {
            if (rgbColorCode[i] != ',' && rgbColorCode[i] != ']')
                continue;

            var span = rgbColorCode[startIndex..i];
            if (!int.TryParse(span, out int color))
            {
                colorIndex++;
                continue;
            }

            switch (colorIndex)
            {
                case 0:
                    r = color;
                    break;
                case 1:
                    g = color;
                    break;
                case 2:
                    b = color;
                    break;
            }

            startIndex = i + 1;
            colorIndex++;
        }

        return Color.FromArgb(
            (byte)MathHelper.Clamp(r, 0, 255),
            (byte)MathHelper.Clamp(g, 0, 255),
            (byte)MathHelper.Clamp(b, 0, 255));
    }


    private static Dimension CalculateDrawArea(List<RenderableSentence> sentences)
    {
        if (sentences.Count == 0)
            return default;

        // We want to pick the largest X, but sum up the Y.
        Vec2I point = Vec2I.Zero;
        foreach (var sentence in sentences)
        {
            if (sentence.DrawArea.Vector.X > point.X)
                point.X = sentence.DrawArea.Vector.X;

            point.Y += sentence.DrawArea.Vector.Y;
        }

        return (point.X, point.Y);
    }

    private void AlignTo(TextAlign align)
    {
        // If it's not any of these, then we're done.
        switch (align)
        {
        case TextAlign.Center:
            AlignCenter();
            break;
        case TextAlign.Right:
            AlignRight();
            break;
        }
    }

    private void AlignCenter()
    {
        foreach (RenderableSentence sentence in Sentences)
        {
            int gutter = (DrawArea.Width - sentence.DrawArea.Width) / 2;
            AdjustOffsetsBy(sentence, gutter);
        }
    }

    private void AlignRight()
    {
        foreach (RenderableSentence sentence in Sentences)
        {
            int gutter = DrawArea.Width - sentence.DrawArea.Width;
            AdjustOffsetsBy(sentence, gutter);
        }
    }

    private static void AdjustOffsetsBy(RenderableSentence sentence, int pixelAdjustmentWidth)
    {
        // I am afraid of ending up with copies because this is a
        // struct, so I'll do this to make sure we don't have bugs.
        //foreach (RenderedGlyph glyph in sentence.Glyphs)
        for (int i = 0; i < sentence.Glyphs.Count; i++)
        {
            RenderableGlyph glyph = sentence.Glyphs[i];

            ImageBox2I pos = glyph.Coordinates;
            ImageBox2I newCoordinate = new(pos.Left + pixelAdjustmentWidth, pos.Top, pos.Right, pos.Bottom);
            sentence.Glyphs[i] = new RenderableGlyph(glyph.Character, newCoordinate, glyph.Location, glyph.UV, glyph.Color);
        }
    }

    private void RecalculateGlyphLocations()
    {
        // It is easier to do this after the fact. Doing it during glyph
        // construction would require a ton of reading ahead, alignment,
        // and calculations which would complicate the code. Instead, we
        // do one final recalculation of the normalized coordinates here.
        Vec2D inverse = new(1.0 / DrawArea.Width, 1.0 / DrawArea.Height);

        foreach (RenderableSentence sentence in Sentences)
        {
            for (int i = 0; i < sentence.Glyphs.Count; i++)
            {
                RenderableGlyph renderGlyph = sentence.Glyphs[i];

                ImageBox2I coordinates = renderGlyph.Coordinates;
                Vec2D topLeft = coordinates.Min.Double * inverse;
                Vec2D bottomRight = coordinates.Max.Double * inverse;
                ImageBox2D location = new(topLeft, bottomRight);

                sentence.Glyphs[i] = new RenderableGlyph(renderGlyph, location);
            }
        }
    }

    public override string ToString()
    {
        return string.Join("\n", Sentences.Select(s => s.ToString()));
    }

    /// <summary>
    /// A range for a color in a string.
    /// </summary>
    internal struct ColorRange
    {
        /// <summary>
        /// The start index.
        /// </summary>
        public int StartIndex;

        /// <summary>
        /// The end index.
        /// </summary>
        public int EndIndex;

        /// <summary>
        /// The color for the range.
        /// </summary>
        public Color Color;

        public ColorRange(int index, Color color)
        {
            StartIndex = index;
            EndIndex = index;
            Color = color;
        }
    }
}
