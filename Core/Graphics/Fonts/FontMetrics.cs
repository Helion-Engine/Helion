using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.Fonts
{
    /// <summary>
    /// A collection of information for a font.
    /// </summary>
    public struct FontMetrics
    {
        /// <summary>
        /// The size of the font when creating it.
        /// </summary>
        public int FontSize { get; }

        /// <summary>
        /// The maximum height of all the glyphs.
        /// </summary>
        public int MaxHeight { get; }

        /// <summary>
        /// The max distance from the top of the font to the baseline.
        /// </summary>
        public int MaxAscent { get; }

        /// <summary>
        /// The max distance from the baseline to the font bottom.
        /// </summary>
        public int MaxDescent { get; }

        /// <summary>
        /// A 'recommended' pixel height for line spacing.
        /// </summary>
        public int LineSkip { get; }

        public FontMetrics(int fontSize, int maxHeight, int maxAscent, int maxDescent, int lineSkip)
        {
            Precondition(maxHeight > 0, "Font must have a positive height");
            
            FontSize = fontSize;
            MaxHeight = maxHeight;
            MaxAscent = maxAscent;
            MaxDescent = maxDescent;
            LineSkip = lineSkip;
        }
    }
}
