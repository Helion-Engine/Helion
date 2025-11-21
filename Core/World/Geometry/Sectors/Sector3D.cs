using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Geometry.Lines;
using System;

namespace Helion.World.Geometry.Sectors;

public class Sector3D
{
    public Sector Sector;
    public SectorPlane Ceiling;
    public SectorPlane Floor;
    public Line[] Lines;
    public Entity Entity;

    public Sector3D(IWorld world, Sector sector, Line[] lines)
    {
        Sector = sector;
        Ceiling = sector.Ceiling;
        Floor = sector.Floor;
        Lines = lines;
        Entity = new();
        Entity.Set(-1, -1, 0, EntityDefinition.Default, default, 0, Sector.Default, world, default);
        Entity.Sector3D = this;
        Entity.Flags.SetSolid();
        Entity.Flags.SetActLikeBridge();
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
