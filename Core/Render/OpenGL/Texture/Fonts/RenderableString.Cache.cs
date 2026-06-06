using Helion.Graphics;
using Helion.Graphics.Fonts;
using Helion.Render.Common.Enums;
using Helion.Util.Container;
using System;

namespace Helion.Render.OpenGL.Texture.Fonts;

public partial class RenderableString
{
    private static readonly DynamicArray<RenderableString> StringCache = new(64);
    private static readonly DynamicArray<DynamicArray<ColorRange>> ColorRangeCache = new(32);
    private static readonly DynamicArray<DynamicArray<RenderableGlyph>> GlyphsCache = new(256);
    private static readonly DynamicArray<DynamicArray<RenderableSentence>> SentencesCache = new(64);

    public static RenderableString Get(ReadOnlySpan<char> str, Font font, int fontSize, TextAlign align = TextAlign.Left,
        int maxWidth = int.MaxValue, Color? drawColor = null)
    {
        lock (StringCache)
        {
            if (StringCache.Length > 0)
            {
                var renderableString = StringCache.RemoveLast();
                renderableString.Set(str, font, fontSize, align, maxWidth, drawColor);
                return renderableString;
            }
        }

        return new RenderableString(str, font, fontSize, align, maxWidth, drawColor);
    }

    public void Free()
    {
        if (!ShouldFree)
            return;

        FreeData();
        StringCache.Add(this);
    }

    private void FreeData()
    {
        for (int i = 0; i < Sentences.Length; i++)
            FreeGlyphs(Sentences[i].Glyphs);
        FreeSentences(Sentences);

        Sentences = null!;
        Font = null!;
    }

    private static DynamicArray<ColorRange> GetColorRange()
    {
        lock (ColorRangeCache)
        {
            if (ColorRangeCache.Length > 0)
                return ColorRangeCache.RemoveLast();
        }

        return new DynamicArray<ColorRange>(32);
    }

    private static void FreeColorRange(DynamicArray<ColorRange> colors)
    {
        colors.Clear();
        lock (ColorRangeCache)
        {
            ColorRangeCache.Add(colors);
        }
    }

    private static DynamicArray<RenderableSentence> GetSentences()
    {
        lock (SentencesCache)
        {
            if (SentencesCache.Length > 0)
                return SentencesCache.RemoveLast();
        }

        return new DynamicArray<RenderableSentence>();
    }

    private static void FreeSentences(DynamicArray<RenderableSentence> list)
    {
        list.Clear();
        lock (SentencesCache)
        {
            SentencesCache.Add(list);
        }
    }

    private static DynamicArray<RenderableGlyph> GetGlyphs()
    {
        lock (GlyphsCache)
        {
            if (GlyphsCache.Length > 0)
                return GlyphsCache.RemoveLast();
        }

        return new DynamicArray<RenderableGlyph>(256);
    }

    private static void FreeGlyphs(DynamicArray<RenderableGlyph> list)
    {
        list.Clear();
        lock (GlyphsCache)
        {
            GlyphsCache.Add(list);
        }
    }
}
