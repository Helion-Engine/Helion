namespace Helion.Graphics.Fonts
{
    /// <summary>
    /// A renderable font.
    /// </summary>
    public interface IFont
    {
        public const char DefaultChar = '?';
        
        /// <summary>
        /// The name of the font.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// How many pixels tall the font is. This value is to be used for any
        /// scaling of the font sizes.
        /// </summary>
        public int MaxHeight { get; }
        
        /// <summary>
        /// The metrics of the font (primarily for TTF's).
        /// </summary>
        public FontMetrics Metrics { get; }
        
        /// <summary>
        /// The backing texture that contains all of the glyphs. When getting
        /// glyphs from this font, their positions will map onto this atlas.
        /// </summary>
        public Image Image { get; }
        
        /// <summary>
        /// Gets the glyph for the character, or returns the default character
        /// glyph.
        /// </summary>
        /// <param name="c">The character to get.</param>
        /// <returns>The glyph for the character.</returns>
        public Glyph Get(char c);
        
        /// <summary>
        /// Tries to get the glyph, if it exists.
        /// </summary>
        /// <param name="c">The character to find.</param>
        /// <param name="glyph">The glyph for the character, or null if there
        /// is no character mapping.</param>
        /// <returns>True if found, false if not.</returns>
        public bool TryGet(char c, out Glyph glyph);
    }
}