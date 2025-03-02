using Helion.Geometry.Vectors;
using Helion.Maps.Components;
using Helion.Util;

namespace Helion.Maps.Udmf.Components;

public class UdmfSide : ISide
{
    public int Id { get; set; }

    public Vec2F UpperOffset { get; set; }
    public Vec2F MiddleOffset { get; set; }
    public Vec2F BottomOffset { get; set; }
    public Vec2F UpperScale { get; set; }
    public Vec2F MiddleScale { get; set; }
    public Vec2F BottomScale { get; set; }

    public string UpperTexture { get; set; } = Constants.NoTexture;

    public string MiddleTexture { get; set; } = Constants.NoTexture;

    public string LowerTexture { get; set; } = Constants.NoTexture;

    public UdmfSector Sector { get; set; } = null!;

    public ISector GetSector() => Sector;
}
