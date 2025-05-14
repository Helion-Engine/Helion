using System;
using Helion.Util.Assertion;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Physics;

/// <summary>
/// Represents the opening space for a line segment.
/// </summary>
public class LineOpening
{
    public double CeilingZ;
    public double FloorZ;
    public double OpeningHeight;
    public double DropOffZ;
    public Sector? FloorSector;
    public Sector? CeilingSector;

    public static double GetOpeningHeight(Line line)
    {
        Assert.Precondition(line.Back != null, "Cannot create LineOpening with one sided line");

        return Math.Min(line.Front.Sector.Ceiling.Z, line.Back!.Sector.Ceiling.Z) -
            Math.Max(line.Front.Sector.Floor.Z, line.Back!.Sector.Floor.Z);
    }

    public LineOpening()
    {
        CeilingZ = 0;
        FloorZ = 0;
        DropOffZ = 0;
        OpeningHeight = 0;
        FloorSector = null;
        CeilingSector = null;
    }

    public void Set(Sector front, Sector back)
    {
        Assert.Precondition(back != null, "Cannot create LineOpening with one sided line");

        if (front.Ceiling.Z < back!.Ceiling.Z)
        {
            CeilingZ = front.Ceiling.Z;
            CeilingSector = front;
        }
        else
        {
            CeilingZ = back.Ceiling.Z;
            CeilingSector = back;
        }

        if (front.Floor.Z > back.Floor.Z)
        {
            FloorZ = front.Floor.Z;
            FloorSector = front;
        }
        else
        {
            FloorZ = back.Floor.Z;
            FloorSector = back;
        }

        OpeningHeight = CeilingZ - FloorZ;
    }


    public void SetTop(TryMoveData tryMove, Entity other, bool missileClipCompat)
    {
        CeilingZ = tryMove.LowestCeilingZ;
        double otherHeight = missileClipCompat ? other.GetMissileClipHeight(missileClipCompat) : other.Height;
        FloorZ = other.Position.Z + otherHeight;
        OpeningHeight = CeilingZ - FloorZ;
        DropOffZ = FloorZ;
        CeilingSector = null;
        FloorSector = null;
    }

    public void SetBottom(TryMoveData tryMove, Entity other)
    {
        CeilingZ = other.Position.Z;
        FloorZ = tryMove.HighestFloorZ;
        OpeningHeight = CeilingZ - FloorZ;
        DropOffZ = FloorZ;
        CeilingSector = null;
        FloorSector = null;
    }

    public bool CanStepUpInto(Entity entity)
    {
        return entity.Position.Z < FloorZ && entity.Position.Z >= FloorZ - entity.GetMaxStepHeight();
    }

    public bool Fits(Entity entity) => entity.Height <= OpeningHeight;

    public bool CanPassOrStepThrough(Entity entity)
    {
        if (!Fits(entity) || entity.Position.Z + entity.Height > CeilingZ)
            return false;

        if (entity.Position.Z < FloorZ)
            return CanStepUpInto(entity);

        return true;
    }
}
