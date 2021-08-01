using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry;
using Helion.Graphics.Geometry;

namespace Helion.Render.Legacy.Texture.Fonts
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
            return glyphs.Select(g => g.Coordinates)
                         .Aggregate((acc, glyphLoc) =>
                         {
                             int x = Math.Max(acc.Width, glyphLoc.Right);
                             int y = Math.Max(acc.Height, glyphLoc.Height);
                             return new ImageBox2I(0, 0, x, y);
                         })
                         .ToDimension();
        }

        public override string ToString() => new(Glyphs.Select(g => g.Character).ToArray());
    }
}
