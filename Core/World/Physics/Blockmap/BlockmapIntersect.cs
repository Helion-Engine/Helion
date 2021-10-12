using Helion.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;

namespace Helion.World.Physics.Blockmap;

public struct BlockmapIntersect
{
    public Entity? Entity;
    public Line? Line;
    public Vec2D Intersection;
    public double Distance2D;

    public BlockmapIntersect(Entity entity, Vec2D intersection, double distance2D)
    {
        Entity = entity;
        Line = null;
        Intersection = intersection;
        Distance2D = distance2D;
    }

    public BlockmapIntersect(Line line, Vec2D intersection, double distance2D)
    {
        Entity = null;
        Line = line;
        Intersection = intersection;
        Distance2D = distance2D;
    }
}

