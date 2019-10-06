using Helion.Util.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Physics
{
    public struct BlockmapIntersect
    {
        public Entity? Entity;
        public Line? Line;
        public Sector? Sector;
        public Vec2D Intersection;
        public double Distance2D;

        public BlockmapIntersect(Entity entity, Vec2D intersection, double distance2D)
        {
            Entity = entity;
            Line = null;
            Sector = null;
            Intersection = intersection;
            Distance2D = distance2D;
        }

        public BlockmapIntersect(Line line, Vec2D intersection, double distance2D)
        {
            Entity = null;
            Line = line;
            Sector = null;
            Intersection = intersection;
            Distance2D = distance2D;
        }
    }
}
