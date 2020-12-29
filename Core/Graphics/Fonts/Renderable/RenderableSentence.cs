using System.Collections.Generic;
using Helion.Util.Geometry;

namespace Helion.Graphics.Fonts.Renderable
{
    /// <summary>
    /// A single sentence. We define a sentence as a single line of horizontal
    /// characters, meaning it's not an actual sentence ended by a period, but
    /// rather a single line of characters.
    /// </summary>
    public class RenderableSentence
    {
        /// <summary>
        /// The enclosing box around all the glyphs.
        /// </summary>
        public readonly Dimension DrawArea;

        /// <summary>
        /// The glyphs and their draw positions.
        /// </summary>
        public readonly List<RenderableGlyph> Glyphs;

        public RenderableSentence(List<RenderableGlyph> glyphs)
        {
            Glyphs = glyphs;
            DrawArea = CalculateDrawArea(glyphs);
        }

        private static Dimension CalculateDrawArea(List<RenderableGlyph> glyphs)
        {
            // TODO
            return default;
        }
    }
}
