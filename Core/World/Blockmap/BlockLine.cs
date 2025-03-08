using Helion.Geometry.Segments;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using System;

namespace Helion.World.Blockmap;

public struct BlockLine(int blockIndex, in Seg2D segment, Line line, bool oneSided, Sector frontSector, Sector? backSector) : IComparable<BlockLine>
{
    public int BlockIndex = blockIndex;
    public int LineId = line.Id;
    public Seg2D Segment = segment;
    public LineBlockFlags BlockFlags = line.Flags.Blocking;
    public bool OneSided = oneSided;
    public bool HasSpecial = line.HasSpecial;
    public Sector FrontSector = frontSector;
    public Sector? BackSector = backSector;

    public readonly int CompareTo(BlockLine other)
    {
        // Doom added lines to the block in line id order
        // This affects behavior on how block checks will short and needs to be enforced here
        if (BlockIndex == other.BlockIndex)
            return LineId.CompareTo(other.LineId);

        return BlockIndex.CompareTo(other.BlockIndex);
    }
}
