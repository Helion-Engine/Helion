using System;
using System.Collections.Generic;
using System.IO;
using Helion.Bsp;
using Helion.Bsp.Builder.GLBSP;
using Helion.Bsp.External;
using Helion.Maps;
using Helion.Maps.Doom;
using Helion.Maps.Hexen;
using Helion.Resource.Archives.Collection;
using Helion.Resource.Archives.Locator;
using Helion.Util.Configuration;
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
        /// <param name="config">The player config data.</param>
        /// <returns>A map geometry object if it was parsed and created right,
        /// otherwise null if it failed.</returns>
        public static MapGeometry? Create(IMap map, Config config)
        {
            switch (map)
            {
                case DoomMap doomMap:
                    return DoomGeometryBuilder.Create(doomMap, CreateBspBuilder(map, config));
                case HexenMap hexenMap:
                    return HexenGeometryBuilder.Create(hexenMap, CreateBspBuilder(map, config));
                default:
                    Log.Error("Do not support map type {0} yet", map.MapType);
                    return null;
            }
        }

        private static IBspBuilder CreateBspBuilder(IMap map, Config config)
        {
            if (config.Engine.Developer.UseZdbsp)
                return CreateZdbspBuilder(map, config);

            return CreateInternalBspBuilder(map, config);
        }

        private static IBspBuilder? CreateZdbspBuilder(IMap map, Config config)
        {
            if (!ZdbspDownloader.HasZdbsp())
            {
                if (!ZdbspDownloader.Download())
                    Log.Error("Failed to download zdbsp");
            }

            string output = Path.Combine(Directory.GetCurrentDirectory(), ZdbspDownloader.Folder, "temp.wad");
            Zdbsp bsp = new Zdbsp(ZdbspDownloader.BspExePath, map.Archive.Path.FullPath, map.Name, output);
            bsp.Run();

            FilesystemArchiveLocator locator = new FilesystemArchiveLocator();
            ArchiveCollection archiveCollection = new ArchiveCollection(locator);
            if (!archiveCollection.Load(new string[] { output }, false))
            {
                Log.Error($"Failed to load zdbsp output: {output}");
                return null;
            }

            MapEntryCollection? mapEntryCollection = archiveCollection.GetMapEntryCollection(map.Name);
            if (mapEntryCollection == null)
            {
                Log.Error($"Failed to load map from zdbsp output: {output}");
                return null;
            }

            return new GLBspBuilder(map, mapEntryCollection);
        }

        private static IBspBuilder CreateInternalBspBuilder(IMap map, Config config)
        {
            return new BspBuilder(map);
        }
    }
}