using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Special;
using Helion.World.Static;
using System;

namespace Helion.World.Geometry.Sectors;

public enum SectorFlags3D
{
    None = 0,
    Solid = 1,
    Swim = 2,
    LightBetween = 4,
    DisableLighting = 8,
    RenderInside = 16,
    VisibilityInvert = 32,
    ShootabilityInvert = 64,
}

public class Sector3D
{
    public int ParentSectorId;
    public int SectorId;
    public Sector ParentSector;
    public Sector ControlSector;
    public SectorPlane ControlCeiling;
    public SectorPlane ControlFloor;
    public Sector FakeSector;
    public SectorPlane FakeFloor;
    public SectorPlane FakeCeiling;
    public Entity Entity;
    public SectorFlags3D Flags;

    public Sector3D(IWorld world, int parentSectorId, Sector parentSector, Sector controlSector, int textureHandle, SectorFlags3D flags)
    {
        SectorId = world.Geometry.CreateNewSectorId();
        ParentSectorId = parentSectorId;
        FakeFloor = new(SectorPlaneFace.Floor, 0, 0, 0);
        FakeCeiling = new(SectorPlaneFace.Ceiling, 0, 0, 0);
        FakeSector = new(SectorId, 0, 0, FakeFloor, FakeCeiling, default, default)
        {
            Sector3D = this,
            Lines = CreateSector3DLines(world, world.Sectors[parentSectorId], textureHandle)
        };
        ParentSector = parentSector;
        ControlSector = controlSector;
        ControlCeiling = controlSector.Ceiling;
        ControlFloor = controlSector.Floor;
        Flags = flags;
        Entity = new();
        Entity.Set(-1, -1, 0, EntityDefinition.Default, default, 0, Sector.Default, world, default);
        Entity.Sector3D = this;
        Entity.Flags.SetSolid();
        Entity.Flags.SetActLikeBridge();
    }

    public void Reset()
    {
        for (int i = 0; i < FakeSector.Lines.Length; i++)
            FakeSector.Lines[i].Front.Reset();

        FakeFloor.Reset(0);
        FakeCeiling.Reset(0);
    }

    private static Line[] CreateSector3DLines(IWorld world, Sector sector, int textureHandle)
    {
        var lines = new Line[sector.Lines.Length];
        for (int i = 0; i < sector.Lines.Length; i++)
        {
            var line = sector.Lines[i];
            var middle = new Wall(textureHandle, WallLocation.Middle3D);
            var side = new Side(world.Geometry.CreateNewSideId(), line.Front.Offset, line.Front.Upper, middle, line.Front.Lower, sector);
            // Normalize so front is always the rendered side
            var lineSeg = line.Segment;
            if (line.Front.Sector == sector)
                lineSeg = new(lineSeg.End, lineSeg.Start);
            lines[i] = new Line(line.Id, lineSeg, side, null, default, LineSpecial.Default, default);
        }
        return lines;
    }

    public Entity GetSectorEntity3D()
    {
        Entity.PrevPosition.Z = ControlFloor.PrevZ;
        Entity.Position.Z = ControlFloor.Z;
        Entity.Height = Math.Max(ControlCeiling.Z - ControlFloor.Z, 0);
        return Entity;
    }

    public SectorPlane GetOpposingPlane3D(SectorPlaneFace face, double height = double.MinValue)
    {
        if (face == SectorPlaneFace.Floor)
        {
            var sector = GetNextHighestCeiling3D(height);
            if (sector == ParentSector)
                return sector.Ceiling;
            return sector.Floor;
        }
        else
        {
            var sector = GetNextLowestFloor3D(height);
            if (sector == ParentSector)
                return sector.Floor;
            return sector.Ceiling;
        }
    }

    public Sector GetNextHighestCeiling3D(double height = double.MinValue)
    {
        var minFloorAbove = double.MaxValue;
        if (height == double.MinValue)
            height = ControlCeiling.Z;
        Sector3D? minSector = null;
        for (int i = 0; i < ParentSector.Sectors3D.Length; i++)
        {
            var sector = ParentSector.Sectors3D[i];
            if (sector.ControlFloor.Z <= minFloorAbove && sector.ControlFloor.Z >= height)
            {
                minFloorAbove = sector.ControlFloor.Z;
                minSector = sector;
            }
        }

        return minSector?.ControlSector ?? ParentSector;
    }

    public Sector GetNextLowestFloor3D(double height = double.MinValue)
    {
        var maxCeilingBelow = double.MinValue;
        if (height == double.MinValue)
            height = ControlFloor.Z;
        Sector3D? maxSector = null;
        for (int i = 0; i < ParentSector.Sectors3D.Length; i++)
        {
            var sector = ParentSector.Sectors3D[i];
            if (sector.ControlCeiling.Z >= maxCeilingBelow && sector.ControlCeiling.Z <= height)
            {
                maxCeilingBelow = sector.ControlCeiling.Z;
                maxSector = sector;
            }
        }

        return maxSector?.ControlSector ?? ParentSector;
    }

    public override string ToString() => $"3D Sector: {SectorId} ControlId: {ControlSector.Id} ParentId: {ParentSectorId} [{ControlSector.Ceiling.Z} {ControlSector.Floor.Z}]";
}
