using System;

namespace Helion.World.Geometry.Sectors;

public enum SectorPlaneFace
{
    Floor,
    Ceiling,
}

[Flags]
public enum SectorPlanes
{
    None = 0,
    Floor = 1,
    Ceiling = 2
}