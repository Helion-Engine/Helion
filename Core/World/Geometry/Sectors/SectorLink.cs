using System;

namespace Helion.World.Geometry.Sectors;

[Flags]
public enum SectorLinkFlags
{
    Unlink = 0,
    Floor = 1,
    Ceiling = 2,
    FloorMirror = 4,
    CeilingMirror = 8,

    FloorAndCeiling = Floor | Ceiling,
    FloorAndCeilingMirror = Floor | Ceiling | FloorMirror | CeilingMirror,
    FloorAndFloorMirror = Floor | FloorMirror,
    CeilingAndCeilingMirror = Ceiling | CeilingMirror,
    MirrorBoth = FloorMirror | CeilingMirror,

    FloorNormalAndCeilingMirror = Floor | Ceiling | CeilingMirror,
    CeilingNormalAndFloorMirror = Floor | Ceiling | FloorMirror,

    Mask = Floor | Ceiling | FloorMirror | CeilingMirror
}

public struct SectorLink(Sector sector, SectorLinkFlags flags)
{
    public Sector Sector = sector;
    public SectorLinkFlags Flags = flags;
}
