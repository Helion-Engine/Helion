using System.Collections.Generic;
using Helion.World.Bsp;
using Helion.World.Geometry.Builder;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;

namespace Helion.World.Geometry
{
    public class MapGeometry
    {
        public readonly List<Line> Lines;
        public readonly List<Side> Sides;
        public readonly List<Wall> Walls;
        public readonly List<Sector> Sectors;
        public readonly List<SectorPlane> SectorPlanes;
        public readonly BspTree BspTree;

        internal MapGeometry(GeometryBuilder builder, BspTree bspTree)
        {
            Lines = builder.Lines;
            Sides = builder.Sides;
            Walls = builder.Walls;
            Sectors = builder.Sectors;
            SectorPlanes = builder.SectorPlanes;
            BspTree = bspTree;
        }
    }
}