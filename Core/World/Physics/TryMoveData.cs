using Helion.Geometry.Vectors;
using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;

namespace Helion.World.Physics;

public class TryMoveData
{
    public bool Success;
    public bool CanFloat;
    public bool BlockedLineClearsVelocity;
    public double LowestCeilingZ;
    public double HighestFloorZ;
    public double DropOffZ;

    public Sector? HighestFloor;
    public Sector? LowestCeiling;

    public Entity? DropOffEntity;
    public Subsector? Subsector;

    public Entity? BlockingEntity;

    public DynamicArray<Entity> IntersectEntities2D = new(16);
    public DynamicArray<int> IntersectSpecialLines = new(16);
    public DynamicArray<int> ImpactSpecialLines = new(16);
    public DynamicArray<int> IntersectMidTexLines = new(16);
    public DynamicArray<Sector> IntersectSectors = new(16);

    public void Clear()
    {
        CanFloat = false;
        BlockedLineClearsVelocity = true;
        IntersectEntities2D.Clear();
        IntersectSpecialLines.Clear();
        ImpactSpecialLines.Clear();
        IntersectSectors.Clear();
        IntersectMidTexLines.Clear();
        HighestFloorZ = int.MinValue;
        LowestCeilingZ = int.MaxValue;
        DropOffEntity = null;
        Subsector = null;
        BlockingEntity = null;
    }

    public void SetIntersectionData(LineOpening opening)
    {
        if (opening.DropOffZ < DropOffZ)
        {
            DropOffZ = opening.DropOffZ;
            DropOffEntity = null;
        }

        if (opening.FloorZ > HighestFloorZ)
        {
            HighestFloorZ = opening.FloorZ;
            HighestFloor = opening.FloorSector;
        }
        if (opening.CeilingZ < LowestCeilingZ)
        {
            LowestCeilingZ = opening.CeilingZ;
            LowestCeiling = opening.CeilingSector;
        }
    }
}
