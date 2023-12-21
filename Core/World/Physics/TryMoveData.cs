using Helion.Geometry.Vectors;
using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
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

    public Entity? DropOffEntity;
    public Subsector? Subsector;

    public DynamicArray<Entity> IntersectEntities2D = new();
    public DynamicArray<Line> IntersectSpecialLines = new();
    public DynamicArray<Line> ImpactSpecialLines = new();

    public void SetPosition(in Vec2D position)
    {
        Position = position;
        CanFloat = false;
        IntersectEntities2D.Clear();
        IntersectSpecialLines.Clear();
        ImpactSpecialLines.Clear();
        HighestFloorZ = int.MinValue;
        LowestCeilingZ = int.MinValue;
        DropOffEntity = null;
        Subsector = null;
    }

    public void SetIntersectionData(LineOpening opening)
    {
        if (opening.DropOffZ < DropOffZ)
        {
            DropOffZ = opening.DropOffZ;
            DropOffEntity = null;
        }

        if (opening.FloorZ > HighestFloorZ)
            HighestFloorZ = opening.FloorZ;
        if (opening.CeilingZ < LowestCeilingZ)
            LowestCeilingZ = opening.CeilingZ;
    }
}
