using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.New.Fonts
{
    /// <summary>
    /// A collection of glyphs with rendering information.
    /// </summary>
    public class Font : IEnumerable<Glyph>
    {
        public readonly string Name;
        public readonly int MaxHeight;
        public readonly IImage Image;
        private readonly Dictionary<char, Glyph> m_glyphs;
        private readonly Glyph m_defaultGlyph;

        internal Font(string name, Dictionary<char, Glyph> glyphs, IImage image, char defaultChar = IFont.DefaultChar)
        {
            Precondition(!glyphs.Empty(), "Cannot have an empty glyph set for a font");
            
            Name = name;
            m_glyphs = glyphs;
            Image = image;
            MaxHeight = glyphs.Values.Max(g => g.Area.Height);

            if (!glyphs.TryGetValue(defaultChar, out m_defaultGlyph))
                m_defaultGlyph = glyphs.Values.FirstOrDefault();
        }

        public Glyph Get(char c) => TryGet(c, out Glyph result) ? result : m_defaultGlyph;

        public bool TryGet(char c, out Glyph glyph) => m_glyphs.TryGetValue(c, out glyph);
        
        public IEnumerator<Glyph> GetEnumerator() => m_glyphs.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
