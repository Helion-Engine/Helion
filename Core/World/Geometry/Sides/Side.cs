using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Render.Legacy.Renderers.World;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Walls;
using Helion.World.Static;

namespace Helion.World.Geometry.Sides;

public class Side : IRenderObject
{
    public readonly int Id;
    public readonly Sector Sector;
    public readonly Wall Upper;
    public readonly Wall Middle;
    public readonly Wall Lower;
    public Vec2I Offset;
    public Line Line { get; internal set; }
    public Wall[] Walls { get; protected set; }
    public SideDataTypes DataChanges { get; set; }
    public bool DataChanged => DataChanges > 0;
    // This is currently just for the renderer to know for scrolling lines to not cache
    public bool OffsetChanged { get; set; }
    public bool IsStatic => Upper.Dynamic == SectorDynamic.None && Middle.Dynamic == SectorDynamic.None && Lower.Dynamic == SectorDynamic.None;
    public bool IsDynamic => Upper.Dynamic != SectorDynamic.None || Middle.Dynamic != SectorDynamic.None || Lower.Dynamic != SectorDynamic.None;

    public bool IsFront => ReferenceEquals(this, Line.Front);
    public bool IsTwoSided => Line.Back != null;
    public Side? PartnerSide => IsFront ? Line.Back : Line.Front;

    public SideScrollData? ScrollData { get; set; }

    public double RenderDistance { get; set; }
    public RenderObjectType Type => RenderObjectType.Side;
    public int LastRenderGametick;

    public Side(int id, Vec2I offset, Wall upper, Wall middle, Wall lower, Sector sector)
    {
        Id = id;
        Sector = sector;
        Offset = offset;
        Upper = upper;
        Middle = middle;
        Lower = lower;
        Walls = new[] { middle, upper, lower };

        upper.Side = this;
        middle.Side = this;
        lower.Side = this;
        sector.Sides.Add(this);

        // We are okay with things blowing up violently if someone forgets
        // to assign it, because that is such a critical error on the part
        // of the developer if this ever happens that it's deserved. Fixing
        // this would lead to some very messy logic, and when this is added
        // to a parent object, it will add itself for us. If this can be
        // fixed in the future with non-messy code, go for it.
        Line = null !;
    }

    public void SetAllWallsDynamic(SectorDynamic sectorDynamic)
    {
        Upper.Dynamic |= sectorDynamic;
        Lower.Dynamic |= sectorDynamic;
        Middle.Dynamic |= sectorDynamic;
    }

    public void SetWallsDynamic(SideTexture types, SectorDynamic sectorDynamic)
    {
        if (types.HasFlag(SideTexture.Upper))
            Upper.Dynamic |= sectorDynamic;
        if (types.HasFlag(SideTexture.Lower))
            Lower.Dynamic |= sectorDynamic;
        if (types.HasFlag(SideTexture.Middle))
            Middle.Dynamic |= sectorDynamic;
    }

    public void ClearWallsDynamic(SideTexture types, SectorDynamic sectorDynamic)
    {
        if (types.HasFlag(SideTexture.Upper))
            Upper.Dynamic &= ~sectorDynamic;
        if (types.HasFlag(SideTexture.Lower))
            Lower.Dynamic &= ~sectorDynamic;
        if (types.HasFlag(SideTexture.Middle))
            Middle.Dynamic &= ~sectorDynamic;
    }
}
