using System.Collections.Generic;
using Helion.Maps;
using Helion.Maps.Doom;
using Helion.Maps.Hexen;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using NLog;

namespace Helion.World.Geometry.Builder
{
    public class GeometryBuilder
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly List<Line> Lines = new List<Line>();
        public readonly List<Side> Sides = new List<Side>();
        public readonly List<Wall> Walls = new List<Wall>();
        public readonly List<Sector> Sectors = new List<Sector>();
        public readonly List<SectorPlane> SectorPlanes = new List<SectorPlane>();
        
        internal GeometryBuilder()
        {
        }

        public static MapGeometry? Create(IMap map)
        {
            switch (map)
            {
            case DoomMap doomMap:
                return DoomGeometryBuilder.Create(doomMap);
            case HexenMap hexenMap:
                return HexenGeometryBuilder.Create(hexenMap);
            default:
                Log.Error("Do not support map type {0} yet", map.MapType);
                return null;
            }
        }
    }
}