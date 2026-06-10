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

    FloorBoth = Floor | FloorMirror,
    CeilingBoth = Ceiling | CeilingMirror,
    Mask = Floor | Ceiling | FloorMirror | CeilingMirror
}

public struct SectorLink(Sector sector, SectorLinkFlags flags)
{
    public Sector Sector = sector;
    public SectorLinkFlags Flags = flags;
}
