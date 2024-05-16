using System;
using Helion.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;

namespace Helion.World.Physics.Blockmap;

public struct BlockmapIntersect : IComparable<BlockmapIntersect>
{
    public Entity? Entity;
    public Line? Line;
    public Vec2D Intersection;
    public double SegTime;

    public BlockmapIntersect(Entity entity, Vec2D intersection, double segTime)
    {
        Entity = entity;
        Line = null;
        Intersection = intersection;
        SegTime = segTime;
    }

    public BlockmapIntersect(Line line, Vec2D intersection, double segTime)
    {
        Entity = null;
        Line = line;
        Intersection = intersection;
        SegTime = segTime;
    }

    public int CompareTo(BlockmapIntersect other)
    {
        return SegTime.CompareTo(other.SegTime);
    }
}
