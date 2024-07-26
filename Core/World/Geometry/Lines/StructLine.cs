using Helion.Geometry.Vectors;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Geometry.Lines;

public readonly record struct StructLine
{
    public readonly Vec2D Start;
    public readonly Vec2D End;
    public readonly Sector FrontSector;
    public readonly Sector? BackSector;
    public readonly Line Line;
    public readonly int LineId;
    public readonly bool BlockSound;

    public StructLine(Line line)
    {
        Start = line.Segment.Start;
        End = line.Segment.End;
        FrontSector = line.Front.Sector;
        BackSector = line.Back?.Sector;
        Line = line;
        LineId = line.LineId;
        BlockSound = line.Flags.BlockSound;
    }
}
