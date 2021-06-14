using Helion.Geometry.Boxes;

namespace Helion.Graphics.New.Fonts
{
    /// <summary>
    /// Information for some character inside an image.
    /// </summary>
    public readonly struct Glyph
    {
        public readonly char Character;
        public readonly Box2F UV;
        public readonly Box2I Area;

        public Glyph(char character, Box2F uv, Box2I area)
        {
            Character = character;
            UV = uv;
            Area = area;
        }
    }
}
