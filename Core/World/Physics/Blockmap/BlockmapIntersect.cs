using System;
using System.Runtime.CompilerServices;
namespace Helion.World.Physics.Blockmap;

public enum IntersectType
{
    Line,
    Entity
}

public struct BlockmapIntersect : IComparable<BlockmapIntersect>
{
    const int EntityFlagShift = 31;
    public const int EntityFlag = 1 << EntityFlagShift;
    public const int EntityMask = ~EntityFlag;

    // Packs to 8 bytes
    // Index maps to DataCache.Entites or Blockmap.BlockLines
    public int Index;
    public double SegTime;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly IntersectType GetIndex(out int index)
    {
        index = Index & EntityMask;
        return (IntersectType)(((uint)Index & EntityFlag) >> EntityFlagShift);
    }

    public readonly int CompareTo(BlockmapIntersect other)
    {
        return SegTime.CompareTo(other.SegTime);
    }
}
