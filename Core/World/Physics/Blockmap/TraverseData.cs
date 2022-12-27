using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util.Container;
using System.Collections.Generic;

namespace Helion.World.Physics.Blockmap;

internal struct TraverseData
{
    public int CheckCount;
    public Box2D? Box;
    public Seg2D? Seg;
    public Vec2D Center;
    public BlockmapTraverseFlags Flags;
    public BlockmapTraverseEntityFlags EntityFlags;
    public bool StopOnOneSidedLine;
    public bool HitOneSidedLine;
    public DynamicArray<BlockmapIntersect> Intersections;

    public TraverseData(int checkCount, Box2D? box, Seg2D? seg, BlockmapTraverseFlags flags, BlockmapTraverseEntityFlags entityFlags, bool stopOnOneSidedLine,
        DynamicArray<BlockmapIntersect> intersections, Vec2D center)
    {
        CheckCount = checkCount;
        Box = box;
        Seg = seg;
        Flags = flags;
        EntityFlags = entityFlags;
        StopOnOneSidedLine = stopOnOneSidedLine;
        Intersections = intersections;
        Center = center;
    }
}
