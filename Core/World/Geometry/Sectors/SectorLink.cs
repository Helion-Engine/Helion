namespace Helion.World.Geometry.Sectors;

public enum SectorLinkFlags
{
    Unlink = 0,
    Floor = 1,
    Ceiling = 2,
    FloorOpposite = 4,
    CeilingOpposite = 8
}

public struct SectorLink(Sector sector, SectorLinkFlags flags)
{
    public Sector Sector = sector;
    public SectorLinkFlags Flags = flags;
}
