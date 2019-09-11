using System.Collections.Generic;
using System.Drawing;

namespace Helion.Graphics.Fonts.TrueTypeFont
{
    public class TtfFontInfo
    {
        public readonly IList<TtfGlyph> Glyphs;
        public readonly Rectangle Bounds;
        public readonly FontMetrics Metrics;

        public TtfFontInfo(IList<TtfGlyph> glyphs, Rectangle bounds, FontMetrics metrics)
        {
            Glyphs = glyphs;
            Bounds = bounds;
            Metrics = metrics;
        }
    }
}