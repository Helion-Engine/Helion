using Helion.Geometry.Vectors;
using Helion.Maps.Components;

namespace Helion.Maps.Doom.Components;

public class DoomSide : ISide
{
    private Vec2I _offset;
    private Vec2F _upperOffset;
    private Vec2F _middleOffset;
    private Vec2F _lowerOffset;
    private Vec2F _scale = Vec2F.One;

    public int Id { get; }
    public ref Vec2I Offset => ref _offset;
    public ref Vec2F UpperOffset => ref _upperOffset;
    public ref Vec2F MiddleOffset => ref _middleOffset;
    public ref Vec2F BottomOffset => ref _lowerOffset;
    public ref Vec2F UpperScale => ref _scale;
    public ref Vec2F MiddleScale => ref _scale;
    public ref Vec2F BottomScale => ref _scale;
    public int LightLevel { get; }
    public int LightLevelUpper { get; }
    public int LightLevelMiddle { get; }
    public int LightLevelLower { get; }
    public string UpperTexture { get; set; }
    public string MiddleTexture { get; set; }
    public string LowerTexture { get; set; }
    public bool LightLevelAbsolute { get; }
    public bool LightLevelUpperAbsolute { get; }
    public bool LightLevelMiddleAbsolute { get; }
    public bool LightLevelLowerAbsolute { get; }
    public readonly DoomSector Sector;

    public DoomSide(int id, Vec2I offset, string upperTexture, string middleTexture, string lowerTexture,
        DoomSector sector)
    {
        Id = id;
        Offset = offset;
        UpperTexture = upperTexture;
        MiddleTexture = middleTexture;
        LowerTexture = lowerTexture;
        Sector = sector;
    }

    public ISector GetSector() => Sector;
}
