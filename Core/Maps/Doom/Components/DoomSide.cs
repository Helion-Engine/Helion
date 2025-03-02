using Helion.Geometry.Vectors;
using Helion.Maps.Components;

namespace Helion.Maps.Doom.Components;

public class DoomSide : ISide
{
    public int Id { get; }
    public Vec2F Offset { get; set; }
    public Vec2F UpperOffset => Offset;
    public Vec2F MiddleOffset => Offset;
    public Vec2F BottomOffset => Offset;
    public Vec2F UpperScale => Vec2F.One;
    public Vec2F MiddleScale => Vec2F.One;
    public Vec2F BottomScale => Vec2F.One;
    public string UpperTexture { get; set; }
    public string MiddleTexture { get; set; }
    public string LowerTexture { get; set; }
    public readonly DoomSector Sector;

    public DoomSide(int id, Vec2I offset, string upperTexture, string middleTexture, string lowerTexture,
        DoomSector sector)
    {
        Id = id;
        Offset = offset.Float;
        UpperTexture = upperTexture;
        MiddleTexture = middleTexture;
        LowerTexture = lowerTexture;
        Sector = sector;
    }

    public ISector GetSector() => Sector;
}
