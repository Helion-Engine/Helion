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
    public int TagSectorId;
    public int SectorId;
    public Sector ControlSector;
    public SectorPlane ControlCeiling;
    public SectorPlane ControlFloor;
    public Sector Sector;
    public SectorPlane Floor;
    public SectorPlane Ceiling;
    public Line[] Lines;
    public Entity Entity;
    public SectorFlags3D Flags;
    public StaticGeometryData Static;

    public Sector3D(IWorld world, int tagSectorId, Sector controlSector, int textureHandle, SectorFlags3D flags)
    {
        SectorId = world.CreateNewSectorId();
        TagSectorId = tagSectorId;
        Floor = new(SectorPlaneFace.Floor, 0, 0, 0);
        Ceiling = new(SectorPlaneFace.Ceiling, 0, 0, 0);
        Sector = new(SectorId, 0, 0, Floor, Ceiling, default, default);
        ControlSector = controlSector;
        ControlCeiling = controlSector.Ceiling;
        ControlFloor = controlSector.Floor;
        Lines = CreateSector3DLines(world, world.Sectors[tagSectorId], textureHandle);
        Flags = flags;
        Entity = new();
        Entity.Set(-1, -1, 0, EntityDefinition.Default, default, 0, Sector.Default, world, default);
        Entity.Sector3D = this;
        Entity.Flags.SetSolid();
        Entity.Flags.SetActLikeBridge();
    }

    public void Reset()
    {
        for (int i = 0; i < Lines.Length; i++)
            Lines[i].Front.Reset();

        Floor.Reset(0);
        Ceiling.Reset(0);
    }

    private static Line[] CreateSector3DLines(IWorld world, Sector sector, int textureHandle)
    {
        var lines = new Line[sector.Lines.Length];
        for (int i = 0; i < sector.Lines.Length; i++)
        {
            var line = sector.Lines[i];
            var middle = new Wall(textureHandle, WallLocation.Middle3D);
            var side = new Side(world.CreateNewSideId(), line.Front.Offset, line.Front.Upper, middle, line.Front.Lower, sector);
            // Normalize so front is always the rendered side
            var lineSeg = line.Segment;
            if (line.Front.Sector == sector)
                lineSeg = new(lineSeg.End, lineSeg.Start);
            lines[i] = new Line(world.CreateNewLineId(), lineSeg, side, null, default, LineSpecial.Default, default);
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

    public override string ToString() => $"{ControlSector.Id}";
}
