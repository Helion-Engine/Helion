using Helion.Maps.Entries;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Util;
using System.Collections.Generic;

namespace Helion.Maps
{
    /// <summary>
    /// A map that contains all the geometry, things, and anything else needed
    /// for an editor or a world.
    /// </summary>
    public class Map
    {
        public CIString Name;
        public List<Line> Lines = new List<Line>();
        public List<Side> Sides = new List<Side>();
        public List<Sector> Sectors = new List<Sector>();
        public List<SectorFlat> SectorFlats = new List<SectorFlat>();
        public List<Vertex> Vertices = new List<Vertex>();

        private Map(CIString name) => Name = name;

        public static Map? From(MapEntryCollection mapEntryCollection)
        {
            Map map = new Map(mapEntryCollection.Name);

            if (!MapEntryReader.ReadInto(mapEntryCollection, map))
                return null;

            return map;
        }
    }
}
