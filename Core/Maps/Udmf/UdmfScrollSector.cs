using Helion.Geometry.Vectors;
using Helion.World.Geometry.Sectors;

namespace Helion.Maps.Udmf;

public enum UdmfScrollSectorFlags
{
    None = 0,
    Texture = 1,
    CarryStaticObjects = 2,
    CarryPlayers = 4,
    CarryMonsters = 8
}

public class UdmfScrollSector(int sectorId, SectorPlaneFace face)
{
    public int SectorId = sectorId;
    public SectorPlaneFace Face = face;
    public UdmfScrollSectorFlags Flags;
    public Vec2D Speed;
}
