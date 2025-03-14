using Helion.Geometry.Vectors;
using Helion.Maps.Components;
using Helion.Util;

namespace Helion.Maps.Udmf.Components;

public class UdmfSide : ISide
{
    private Vec2I _offset;
    private Vec2F _upperOffset;
    private Vec2F _middleOffset;
    private Vec2F _lowerOffset;
    private Vec2F _upperScale = Vec2F.One;
    private Vec2F _middleScale = Vec2F.One;
    private Vec2F _lowerScale = Vec2F.One;

    public int SectorId;
    public int Id { get; set; }
    public ref Vec2I Offset => ref _offset;
    public ref Vec2F UpperOffset => ref _upperOffset;
    public ref Vec2F MiddleOffset => ref _middleOffset;
    public ref Vec2F BottomOffset => ref _lowerOffset;
    public ref Vec2F UpperScale => ref _upperScale;
    public ref Vec2F MiddleScale => ref _middleScale;
    public ref Vec2F BottomScale => ref _lowerScale;
    public int LightLevel { get; set; }
    public int LightLevelUpper { get; set; }
    public int LightLevelMiddle { get; set; }
    public int LightLevelLower { get; set; }
    public string UpperTexture { get; set; } = Constants.NoTexture;
    public string MiddleTexture { get; set; } = Constants.NoTexture;
    public string LowerTexture { get; set; } = Constants.NoTexture;
    public bool LightLevelAbsolute { get; set; }
    public bool LightLevelUpperAbsolute { get; set; }
    public bool LightLevelMiddleAbsolute { get; set; }
    public bool LightLevelLowerAbsolute { get; set; }
    public bool NoFakeConstrast { get; set; }
    public bool SmoothLighting { get; set; }
    public bool WrapMidTex { get; set; }

    public UdmfSector Sector = null!;

    public ISector GetSector() => Sector;
}
