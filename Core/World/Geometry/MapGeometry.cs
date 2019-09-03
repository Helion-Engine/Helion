using System.Collections.Generic;
using Helion.World.Bsp;
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
        public readonly List<SectorSpan> SectorSpans;
        public readonly List<SectorPlane> SectorPlanes;
        public readonly BspTree BspTree;

        internal MapGeometry(List<Line> lines, List<Side> sides, List<Wall> walls, List<Sector> sectors, 
            List<SectorSpan> sectorSpans, List<SectorPlane> sectorPlanes, BspTree bspTree)
        {
            Lines = lines;
            Sides = sides;
            Walls = walls;
            Sectors = sectors;
            SectorSpans = sectorSpans;
            SectorPlanes = sectorPlanes;
            BspTree = bspTree;
        }
    }
}