using System.Collections;
using System.Collections.Generic;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.Fonts
{
    /// <summary>
    /// A collection of a set of glyphs for some font.
    /// </summary>
    public class Font : IEnumerable<Glyph>
    {
        /// <summary>
        /// The metrics for the entire font.
        /// </summary>
        public readonly FontMetrics Metrics;

        /// <summary>
        /// The default glyph for this font.
        /// </summary>
        public readonly Glyph DefaultGlyph;
        
        private readonly Dictionary<char, Glyph> m_glyphs = new Dictionary<char, Glyph>();

        /// <summary>
        /// Gets how many glyphs are present.
        /// </summary>
        public int Count => m_glyphs.Count;
        
        /// <summary>
        /// Creates a new font from a series of glyphs.
        /// </summary>
        /// <param name="defaultGlyph">The default glyph.</param>
        /// <param name="glyphs">A list of all the glyphs. This must contain
        /// the default glyph.</param>
        /// <param name="metrics">The font metrics for drawing with.</param>
        public Font(Glyph defaultGlyph, List<Glyph> glyphs, FontMetrics metrics)
        {
            Precondition(!glyphs.Empty(), "Cannot make a font that has no glyphs");
            
            DefaultGlyph = defaultGlyph;
            m_glyphs[defaultGlyph.Character] = defaultGlyph;
            glyphs.ForEach(g => m_glyphs[g.Character] = g);
            Metrics = metrics;
        }

        /// <summary>
        /// Gets the glyph for the byte. If it does not have a glyph mapping
        /// then a default value is returned.
        /// </summary>
        /// <param name="b">The byte.</param>
        /// <returns>The glyph for the byte.</returns>
        public Glyph this[byte b] => m_glyphs.GetValueOrDefault((char)b, DefaultGlyph);

        /// <summary>
        /// Gets the glyph for the char. If it does not have a glyph mapping
        /// then a default value is returned.
        /// </summary>
        /// <param name="c">The char.</param>
        /// <returns>The glyph for the char.</returns>
        public Glyph this[char c] => m_glyphs.GetValueOrDefault(c, DefaultGlyph);

        /// <summary>
        /// Checks if the font has the character as a glyph.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if so, false otherwise.</returns>
        public bool HasCharacter(char c) => m_glyphs.ContainsKey(c);

        public IEnumerator<Glyph> GetEnumerator() => m_glyphs.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
