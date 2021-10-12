using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;

namespace Helion.Graphics.Fonts;

/// <summary>
/// Information for some character inside an image.
/// </summary>
public readonly struct Glyph
{
    public readonly char Character;
    // These are anchored at the top left in an image-based coordinate system.
    public readonly Box2F UV;
    public readonly Box2I Area;

    public Glyph(char character, Box2F uv, Box2I area)
    {
        Character = character;
        UV = uv;
        Area = area;
    }

    public Glyph(char character, Vec2I topLeft, Dimension area, Dimension atlasArea)
    {
        Character = character;
        Area = (topLeft, topLeft + (area.Width, area.Height));

        Vec2F totalArea = atlasArea.Vector.Float;
        Vec2F uvStart = Area.Min.Float / totalArea;
        Vec2F uvEnd = Area.Max.Float / totalArea;
        UV = (uvStart, uvEnd);
    }

    public override string ToString() => $"{Character}, Area: {Area}, UV: {UV}";
}

