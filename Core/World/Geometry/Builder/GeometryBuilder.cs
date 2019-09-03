using System.Collections.Generic;
using Helion.Maps;
using Helion.Maps.Doom;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using NLog;

namespace Helion.World.Geometry.Builder
{
    public abstract class GeometryBuilder
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        public readonly List<Line> Lines;
        public readonly List<Side> Sides;
        public readonly List<Wall> Walls;
        public readonly List<Sector> Sectors;
        public readonly List<SectorSpan> SectorSpans;
        public readonly List<SectorPlane> SectorPlanes;
        public readonly BspTree BspTree;

        protected GeometryBuilder(List<Line> lines, List<Side> sides, List<Wall> walls, List<Sector> sectors, 
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

        public GeometryBuilder? Create(IMap map)
        {
            switch (map)
            {
            case DoomMap doomMap:
                return DoomGeometryBuilder.Create(doomMap);
            default:
                Log.Error("Do not support map type {0} yet", map.MapType);
                return null;
            }
        }
    }
}