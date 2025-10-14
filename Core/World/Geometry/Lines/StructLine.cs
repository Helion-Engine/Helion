using Helion.Geometry.Segments;
using Helion.World.Geometry.Sectors;
using Helion.World.Special;
using System;

namespace Helion.World.Geometry.Lines;

[Flags]
public enum StructLineFlags
{
    BlockSound = 1,
    Secret = 2,
    SeenForAutomap = 4,
    MarkAutomap = 8,
    HasSpecial = 16
}

public record struct StructLine
{
    public int Id;
    public Seg2D Segment;
    public Sector FrontSector;
    public Sector? BackSector;
    public SectorPlane FrontFloorPlane;
    public SectorPlane FrontCeilingPlane;
    public SectorPlane? BackFloorPlane;
    public SectorPlane? BackCeilingPlane;
    public Line Line;
    public int LockKey;
    public StructLineFlags Flags;
    public LineAutomapFlags AutomapFlags;

    public readonly bool BlockSound => (Flags & StructLineFlags.BlockSound) != 0;
    public readonly bool Secret => (Flags & StructLineFlags.Secret) != 0;
    public readonly bool SeenForAutomap => (Flags & StructLineFlags.SeenForAutomap) != 0;
    public readonly bool MarkAutomap => (Flags & StructLineFlags.MarkAutomap) != 0;

    public StructLine(Line line)
    {
        Id = line.Id;
        Segment = line.Segment;
        FrontSector = line.Front.Sector;
        BackSector = line.Back?.Sector;
        FrontFloorPlane = line.Front.Sector.Floor;
        BackFloorPlane = line.Back?.Sector.Floor;
        FrontCeilingPlane = line.Front.Sector.Ceiling;
        BackCeilingPlane = line.Back?.Sector.Ceiling;
        Line = line;

        if (line.SeenForAutomap)
            Flags |= StructLineFlags.SeenForAutomap;
        if (line.Flags.BlockSound)
            Flags |= StructLineFlags.BlockSound;
        if (line.Flags.Secret)
            Flags |= StructLineFlags.Secret;
        if (LockSpecialUtil.IsLockSpecial(line, out int key))
            LockKey = key;
        else
            LockKey = -1;

        if (line.HasSpecial)
            Flags |= StructLineFlags.HasSpecial;

        AutomapFlags = line.Flags.Automap;
    }

    public void Update(Line line)
    {
        if (line.SeenForAutomap)
            Flags |= StructLineFlags.SeenForAutomap;
    }
}
