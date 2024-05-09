using Helion.Geometry.Vectors;
using Helion.Graphics.Palettes;
using Helion.Maps.Specials;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Walls;
using Helion.World.Static;

namespace Helion.World.Geometry.Sides;

public record class SideColormaps(Colormap? Upper, Colormap? Middle, Colormap? Lower);

public record struct FloodKeys(int Key1, int Key2);

public sealed class Side : IRenderObject
{
    public static readonly FloodKeys NoFloodKeys = new(0, 0);

    public readonly int Id;
    public readonly Sector Sector;
    public readonly Wall Upper;
    public readonly Wall Middle;
    public readonly Wall Lower;
    public Vec2I Offset;
    public Line Line { get; internal set; }
    public SideDataTypes DataChanges { get; set; }
    public bool DataChanged => DataChanges > 0;
    // This is currently just for the renderer to know for scrolling lines to not cache
    public bool OffsetChanged => ScrollData != null;
    public bool IsStatic => Upper.Dynamic == SectorDynamic.None && Middle.Dynamic == SectorDynamic.None && Lower.Dynamic == SectorDynamic.None;
    public bool IsDynamic => Upper.Dynamic != SectorDynamic.None || Middle.Dynamic != SectorDynamic.None || Lower.Dynamic != SectorDynamic.None;
    public SideTexture FloodTextures;

    public bool IsFront => ReferenceEquals(this, Line.Front);
    public bool IsTwoSided => Line.Back != null;
    public Side? PartnerSide => IsFront ? Line.Back : Line.Front;

    public SideScrollData? ScrollData { get; set; }

    public SideColormaps? Colormaps { get; set; }

    public double RenderDistanceSquared { get; set; }
    public RenderObjectType Type => RenderObjectType.Side;
    public int LastRenderGametick;
    public int LastRenderGametickAlpha;
    public int BlockmapCount;
    public FloodKeys UpperFloodKeys;
    public FloodKeys LowerFloodKeys;
    public int FloorFloodKey;
    public int CeilingFloodKey;
    public bool BlockmapLinked;
    public SectorPlanes MidTextureFlood;

    private readonly Vec2I m_initialOffset;

    public Side(int id, Vec2I offset, Wall upper, Wall middle, Wall lower, Sector sector)
    {
        Id = id;
        Sector = sector;
        Offset = offset;
        Upper = upper;
        Middle = middle;
        Lower = lower;

        upper.Side = this;
        middle.Side = this;
        lower.Side = this;

        m_initialOffset = offset;

        // We are okay with things blowing up violently if someone forgets
        // to assign it, because that is such a critical error on the part
        // of the developer if this ever happens that it's deserved. Fixing
        // this would lead to some very messy logic, and when this is added
        // to a parent object, it will add itself for us. If this can be
        // fixed in the future with non-messy code, go for it.
        Line = null !;
    }

    public void Reset()
    {
        DataChanges = default;
        ScrollData = default;
        Colormaps = default;
        Offset = m_initialOffset;
        LastRenderGametick = default;
        LastRenderGametickAlpha = default;
        BlockmapCount = default;
        UpperFloodKeys = default;
        LowerFloodKeys = default;
        FloorFloodKey = default;
        CeilingFloodKey = default;
        BlockmapLinked = default;
        MidTextureFlood = default;

        Upper.Reset();
        Middle.Reset();
        Lower.Reset();
    }

    public void SetAllWallsDynamic(SectorDynamic sectorDynamic)
    {
        Upper.Dynamic |= sectorDynamic;
        Lower.Dynamic |= sectorDynamic;
        Middle.Dynamic |= sectorDynamic;
    }

    public void SetWallsDynamic(SideTexture types, SectorDynamic sectorDynamic)
    {
        if ((types & SideTexture.Upper) != 0)
            Upper.Dynamic |= sectorDynamic;
        if ((types & SideTexture.Lower) != 0)
            Lower.Dynamic |= sectorDynamic;
        if ((types & SideTexture.Middle) != 0)
            Middle.Dynamic |= sectorDynamic;
    }

    public void ClearWallsDynamic(SideTexture types, SectorDynamic sectorDynamic)
    {
        if ((types & SideTexture.Upper) != 0)
            Upper.Dynamic &= ~sectorDynamic;
        if ((types & SideTexture.Lower) != 0)
            Lower.Dynamic &= ~sectorDynamic;
        if ((types & SideTexture.Middle) != 0)
            Middle.Dynamic &= ~sectorDynamic;
    }
}
