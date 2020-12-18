using Helion.Util.Geometry.Vectors;
using Helion.Worlds.Geometry.Lines;
using Helion.Worlds.Geometry.Sectors;
using Entity = Helion.Worlds.Entities.Entity;

namespace Helion.Worlds.Physics.Blockmap
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
