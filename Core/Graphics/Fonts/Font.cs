using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.Fonts;

/// <summary>
/// A collection of glyphs with rendering information.
/// </summary>
public class Font : IEnumerable<(char, Glyph)>
{
    public const char DefaultChar = '?';

    public readonly string Name;
    public readonly int MaxHeight;
    public readonly Image Image;
    public readonly bool IsTrueTypeFont;
    private readonly Dictionary<char, Glyph> m_glyphs;
    private readonly Glyph m_defaultGlyph;

    public Font(string name, Dictionary<char, Glyph> glyphs, Image image, char defaultChar = DefaultChar,
        bool isTrueTypeFont = false)
    {
        Precondition(!glyphs.Empty(), "Cannot have an empty glyph set for a font");

        Name = name;
        m_glyphs = glyphs;
        Image = image;
        IsTrueTypeFont = isTrueTypeFont;
        MaxHeight = glyphs.Values.Max(g => g.Area.Height);

        if (!glyphs.TryGetValue(defaultChar, out m_defaultGlyph))
            m_defaultGlyph = glyphs.Values.FirstOrDefault();
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
