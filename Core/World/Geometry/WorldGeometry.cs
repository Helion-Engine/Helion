using Helion.Maps;
using Helion.Util.Geometry;
using System.Collections.Generic;

namespace Helion.World.Geometry
{
    public class WorldGeometry
    {
        public List<Vec2Fixed> Vertices = new List<Vec2Fixed>();
        public List<Line> Lines = new List<Line>();
        public List<Side> Sides = new List<Side>();
        public List<Wall> Walls = new List<Wall>();
        public List<Sector> Sectors = new List<Sector>();
        public List<SectorPlane> SectorPlanes = new List<SectorPlane>();
        public List<Subsector> Subsectors = new List<Subsector>();
        public List<BspNode> Nodes = new List<BspNode>();

        private WorldGeometry()
        {
        }

        public static WorldGeometry From(Map map)
        {
            WorldGeometry geometry = new WorldGeometry();
            
            // TODO

            return geometry;
        }
    }
}
