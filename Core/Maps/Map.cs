using System.Collections.Generic;
using Helion.Maps.Entries;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Maps.Things;
using Helion.Util;

namespace Helion.Maps
{
    /// <summary>
    /// A map that contains all the geometry, things, and anything else needed
    /// for an editor or a world.
    /// </summary>
    public class Map
    {
        public readonly CIString Name;
        public readonly MapType MapType;
        public readonly List<Line> Lines = new List<Line>();
        public readonly List<Thing> Things = new List<Thing>();
        public readonly List<Side> Sides = new List<Side>();
        public readonly List<Sector> Sectors = new List<Sector>();
        public readonly List<SectorFlat> SectorFlats = new List<SectorFlat>();
        public readonly List<Vertex> Vertices = new List<Vertex>();

        private Map(CIString name, MapType type)
        {
            Name = name;
            MapType = type;
        }

        public static Map? From(MapEntryCollection mapEntryCollection)
        {
            Map map = new Map(mapEntryCollection.Name, mapEntryCollection.MapType);

            if (!MapEntryReader.ReadInto(mapEntryCollection, map))
                return null;

            return map;
        }
    }
}