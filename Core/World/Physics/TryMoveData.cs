using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;

namespace Helion.World.Physics;

public class TryMoveData
{
    public bool Success;
    public bool SubMoveSuccess;
    public bool CanFloat;
    public bool BlockedLineClearsVelocity;
    public bool HasTouchy;
    public bool HasDropOff3D;
    public double LowestCeilingZ;
    public double HighestFloorZ;
    // The highest valid floor the entity can step up to. Required to correctly validate 3D block checks so blocking lines are correct. (used for impact lines and player wall sliding)
    // See SetBottom in LineOpening
    public double HighestValidStepFloorZ;
    public double DropOffZ;
    public double DropOffZ_3D;

    public Sector? HighestFloor;
    public Sector? LowestCeiling;
    public Subsector? Subsector;

    public Entity? BlockingEntity;

    public DynamicArray<Entity> IntersectEntities2D = new(16);
    public DynamicArray<int> IntersectSpecialLines = new(16);
    public DynamicArray<int> ImpactSpecialLines = new(16);
    public DynamicArray<int> IntersectMidTexLines = new(16);
    public DynamicArray<Sector> IntersectSectors = new(16, arrayPool: true);

    public void Clear()
    {
        CanFloat = false;
        BlockedLineClearsVelocity = true;
        HasTouchy = false;
        HasDropOff3D = false;
        IntersectEntities2D.Clear();
        IntersectSpecialLines.Clear();
        ImpactSpecialLines.Clear();
        IntersectSectors.Clear();
        IntersectMidTexLines.Clear();
        HighestFloorZ = double.MinValue;
        LowestCeilingZ = double.MaxValue;
        Subsector = null;
        BlockingEntity = null;
        SubMoveSuccess = false;
    }

    public void SetIntersectionData3D(LineOpening opening, Entity entity, bool setDropOff = true)
    {
        HasDropOff3D = HasDropOff3D || opening.HasDropOff3D;

        if (setDropOff && opening.DropOffZ < DropOffZ)
            DropOffZ = opening.DropOffZ;

        if (opening.HasDropOff3D && opening.DropOffZ < DropOffZ_3D)
            DropOffZ_3D = opening.DropOffZ;

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

        if (opening.FloorZ > HighestValidStepFloorZ && opening.FloorZ > entity.Position.Z &&
            opening.FloorZ - entity.Position.Z <= entity.GetMaxStepHeight())
        {
            HighestValidStepFloorZ = opening.FloorZ;
        }
    }
}
