using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;

namespace Helion.Graphics.Fonts.TrueTypeFont
{
    public class TtfGlyph
    {
        public readonly char Character;
        public Vec2I Offset;
        public Dimension Dimension;

        public TtfGlyph(char character, Vec2I offset, Dimension dimension)
        {
            Character = character;
            Offset = offset;
            Dimension = dimension;
        }
    }
}