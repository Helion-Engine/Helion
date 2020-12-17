using System.Collections.Generic;
using Helion.Maps;
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

        public readonly List<Line> Lines = new List<Line>();
        public readonly List<Side> Sides = new List<Side>();
        public readonly List<Wall> Walls = new List<Wall>();
        public readonly List<Sector> Sectors = new List<Sector>();
        public readonly List<SectorPlane> SectorPlanes = new List<SectorPlane>();

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
        public readonly Dictionary<int, Line> MapLines = new Dictionary<int, Line>();

        internal GeometryBuilder()
        {
        }

        /// <summary>
        /// Creates world geometry from a map.
        /// </summary>
        /// <param name="map">The map to turn into world geometry.</param>
        /// <returns>A map geometry object if it was parsed and created right,
        /// otherwise null if it failed.</returns>
        public static MapGeometry? Create(Map map)
        {
            switch (map.MapType)
            {
                case MapType.Doom:
                    return DoomGeometryBuilder.Create(map);
                case MapType.Hexen:
                    return HexenGeometryBuilder.Create(map);
                default:
                    Log.Error("Do not support map type {0} yet", map.MapType);
                    return null;
            }
        }
    }
}