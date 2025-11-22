using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Special;
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
    public Sector Sector;
    public SectorPlane Ceiling;
    public SectorPlane Floor;
    public Line[] Lines;
    public Entity Entity;
    public SectorFlags3D Flags;

    public Sector3D(IWorld world, int tagSectorId, Sector sector, int textureHandle, SectorFlags3D flags)
    {
        TagSectorId = tagSectorId;
        Sector = sector;
        Ceiling = sector.Ceiling;
        Floor = sector.Floor;
        Lines = CreateSector3DLines(world.Sectors[tagSectorId], textureHandle);
        Flags = flags;
        Entity = new();
        Entity.Set(-1, -1, 0, EntityDefinition.Default, default, 0, Sector.Default, world, default);
        Entity.Sector3D = this;
        Entity.Flags.SetSolid();
        Entity.Flags.SetActLikeBridge();
    }

    private static Line[] CreateSector3DLines(Sector sector, int textureHandle)
    {
        var lines = new Line[sector.Lines.Length];
        for (int i = 0; i < sector.Lines.Length; i++)
        {
            var line = sector.Lines[i];
            var middle = new Wall(textureHandle, WallLocation.Middle3D);
            var side = new Side(line.Front.Id, line.Front.Offset, line.Front.Upper, middle, line.Front.Lower, sector);
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
        Entity.PrevPosition.Z = Floor.PrevZ;
        Entity.Position.Z = Floor.Z;
        Entity.Height = Math.Max(Ceiling.Z - Floor.Z, 0);
        return Entity;
    }

    public override string ToString() => $"{Sector.Id}";
}
