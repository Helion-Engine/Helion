using Helion.World.Geometry.Sectors;

namespace Helion.World;

public readonly struct SectorMoveEvent
{
    public readonly Sector Sector;
    public readonly SectorPlane Plane;

    public SectorMoveEvent(Sector sector, SectorPlane plane)
    {
        Sector = sector;
        Plane = plane;
    }
}
