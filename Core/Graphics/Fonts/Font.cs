using System.Collections.Generic;

namespace Helion.Graphics.Fonts
{
    /// <summary>
    /// A collection of a set of glyphs for some font.
    /// </summary>
    public class Font
    {
        /// <summary>
        /// The metrics for the entire font.
        /// </summary>
        public FontMetrics Metrics { get; }

        private readonly Glyph DefaultGlyph;
        private readonly Dictionary<char, Glyph> glyphs = new Dictionary<char, Glyph>();

        public Font(Glyph defaultGlyph, List<Glyph> glyphs, FontMetrics metrics)
        {
            DefaultGlyph = defaultGlyph;
            glyphs[defaultGlyph.C] = defaultGlyph;
            glyphs.ForEach(g => glyphs[g.C] = g);
            Metrics = metrics;
        }

        /// <summary>
        /// Gets the glyph for the byte. If it does not have a glyph mapping
        /// then a default value is returned.
        /// </summary>
        /// <param name="b">The byte.</param>
        /// <returns>The glyph for the byte.</returns>
        public Glyph this[byte b] => glyphs.GetValueOrDefault((char)b, DefaultGlyph);

        /// <summary>
        /// Gets the glyph for the char. If it does not have a glyph mapping
        /// then a default value is returned.
        /// </summary>
        /// <param name="c">The char.</param>
        /// <returns>The glyph for the char.</returns>
        public Glyph this[char c] => glyphs.GetValueOrDefault(c, DefaultGlyph);
    }
}
