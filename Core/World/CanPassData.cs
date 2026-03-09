using Helion.World.Entities;
using Helion.World.Geometry.Sectors;

namespace Helion.World;

internal struct CanPassData
{
    public Entity Entity;
    public Entity? HighestFloorEntity;
    public Entity? LowestCeilingEntity;
    public Sector? CeilingSector3D;
    public double EntityTopZ;
    public double HighestFloorZ;
    public double LowestCeilZ;
    public double LowestCeilLight3D;
    public bool ClampToLinkedSectors;
}
