using Helion.Util.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Graphics.Fonts;

/// <summary>
/// A collection of glyphs with rendering information.
/// </summary>
public class Font : IEnumerable<(char, Glyph)>
{
    public const char DefaultChar = '?';

    public readonly string Name;
    public readonly int MaxHeight;
    public readonly int? FixedWidth;
    public readonly int? FixedHeight;
    public readonly int UpscalingFactor;
    public readonly Glyph? FixedWidthChar;
    public readonly Glyph? FixedWidthNumber;
    public readonly Image Image;
    public readonly bool IsTrueTypeFont;
    private readonly Dictionary<char, Glyph> m_glyphs;
    private readonly Glyph m_defaultGlyph;

    public Font(string name, Dictionary<char, Glyph> glyphs, Image image, char defaultChar = DefaultChar,
        bool isTrueTypeFont = false, int? fixedWidth = null, int? fixedHeight = null, char? fixedWidthChar = null, int upscalingFactor = 1, char? fixedWidthNumber = null)
    {
        Name = name;
        m_glyphs = glyphs;
        Image = image;
        IsTrueTypeFont = isTrueTypeFont;
        if (glyphs.Count > 0)
            MaxHeight = glyphs.Values.Max(g => g.Area.Height);

        if (!glyphs.TryGetValue(defaultChar, out m_defaultGlyph))
            m_defaultGlyph = glyphs.Values.FirstOrDefault();

        UpscalingFactor = upscalingFactor;

        FixedWidth = fixedWidth;
        FixedHeight = fixedHeight;
        if (fixedWidthChar.HasValue && TryGet(fixedWidthChar.Value, out var fixedChar))
            FixedWidthChar = fixedChar;

        if (fixedWidthNumber.HasValue && TryGet(fixedWidthNumber.Value, out var fixedNumber))
            FixedWidthNumber = fixedNumber;
    }

    public Glyph Get(char c) => TryGet(c, out Glyph result) ? result : m_defaultGlyph;

    public bool TryGet(char c, out Glyph glyph) => m_glyphs.TryGetValue(c, out glyph);

    public override string ToString() => $"{Name}, Glyphs: {m_glyphs.Count}, Atlas: {Image.Dimension}";

    public IEnumerator<(char, Glyph)> GetEnumerator()
    {
        foreach ((char k, Glyph v) in m_glyphs)
            yield return (k, v);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
