using Helion.Geometry.Vectors;
using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;

namespace Helion.World.Physics;

public class TryMoveData
{
    public Vec2D Position;
    public bool Success;
    public bool CanFloat;
    public double LowestCeilingZ;
    public double HighestFloorZ;
    public double DropOffZ;

    public Sector? HighestFloor;
    public Sector? LowestCeiling;

    public Entity? DropOffEntity;
    public Subsector? Subsector;

    public Entity? BlockingEntity;
    public Line? BlockingLine;

    public DynamicArray<Entity> IntersectEntities2D = new(128);
    public DynamicArray<Line> IntersectSpecialLines = new(128);
    public DynamicArray<Line> ImpactSpecialLines = new(128);
    public DynamicArray<Sector> IntersectSectors = new();

    public void SetPosition(double x, double y)
    {
        Position.X = x;
        Position.Y = y;
        CanFloat = false;
        IntersectEntities2D.Clear();
        IntersectSpecialLines.Clear();
        ImpactSpecialLines.Clear();
        IntersectSectors.Clear();
        HighestFloorZ = int.MinValue;
        LowestCeilingZ = int.MinValue;
        DropOffEntity = null;
        Subsector = null;
        BlockingEntity = null;
        BlockingLine = null;
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
