using System.Collections.Generic;
using Helion.Bsp;
using Helion.Bsp.Builder.GLBSP;
using Helion.Maps;
using Helion.Maps.Doom;
using Helion.Maps.Hexen;
using Helion.Util.Configs;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using NLog;

namespace Helion.World.Geometry.Builder
{
    /// <summary>
    /// A helper class for making the geometry in a map.
    /// </summary>
    public class GeometryBuilder
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly List<Line> Lines = new();
        public readonly List<Side> Sides = new();
        public readonly List<Wall> Walls = new();
        public readonly List<Sector> Sectors = new();
        public readonly List<SectorPlane> SectorPlanes = new();

        /// <summary>
        /// A cached dictionary that maps the line ID number onto a line from
        /// the map this was parsed from.
        /// </summary>
        /// <remarks>
        /// We need this for the BSP builder to work properly because it does
        /// references by map line ID. It also can't be a dictionary because
        /// the line IDs in a map are not guaranteed to be contiguous due to
        /// map corruption, line removal, etc.
        /// </remarks>
        public readonly Dictionary<int, Line> MapLines = new();

        internal GeometryBuilder()
        {
        }

        /// <summary>
        /// Creates world geometry from a map.
        /// </summary>
        /// <param name="map">The map to turn into world geometry.</param>
        /// <param name="config">The player config data.</param>
        /// <returns>A map geometry object if it was parsed and created right,
        /// otherwise null if it failed.</returns>
        public static MapGeometry? Create(IMap map, Config config)
        {
            IBspBuilder? bspBuilder = CreateBspBuilder(map, config);
            if (bspBuilder == null)
                return null;

            switch (map)
            {
                case DoomMap doomMap:
                    return DoomGeometryBuilder.Create(doomMap, bspBuilder);
                case HexenMap hexenMap:
                    return HexenGeometryBuilder.Create(hexenMap, bspBuilder);
                default:
                    Log.Error("Do not support map type {0} yet", map.MapType);
                    return null;
            }
        }

        private static IBspBuilder? CreateBspBuilder(IMap map, Config config)
        {
            if (config.Developer.InternalBSPBuilder)
                return new BspBuilder(map);

            if (map.GL != null)
                return new GLBspBuilder(map);

            Log.Warn("Unable to find GL nodes from ZDBSP");
            return null;
        }
    }
}