using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        public RenderableSentence(IReadOnlyCollection<RenderableGlyph> glyphs)
        {
            Glyphs = glyphs.ToList();
            DrawArea = CalculateDrawArea(glyphs);
        }

        private static Dimension CalculateDrawArea(IEnumerable<RenderableGlyph> glyphs)
        {
            Rectangle drawArea = glyphs
                .Select(g => g.Location)
                .Aggregate((acc, glyphLoc) =>
                {
                    int x = Math.Max(acc.Width, glyphLoc.Right);
                    int y = Math.Max(acc.Height, glyphLoc.Height);
                    return new Rectangle(0, 0, x, y);
                });

            return new Dimension(drawArea.Width, drawArea.Height);
        }

        public override string ToString() => new(Glyphs.Select(g => g.Character).ToArray());
    }
}
