using Helion.World.Entities;

namespace Helion.World;

internal struct CanPassData
{
    public Entity Entity;
    public Entity HighestFloorEntity;
    public Entity LowestCeilingEntity;
    public double EntityTopZ;
    public double HighestFloorZ;
    public double LowestCeilZ;
    public bool ClampToLinkedSectors;
}
