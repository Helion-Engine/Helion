using Helion.Maps.Entries;
using Helion.Maps.Geometry;
using System.Collections.Generic;

namespace Helion.Maps
{
    public class Map
    {
        public List<Line> Lines = new List<Line>();
        public List<Side> Sides = new List<Side>();
        public List<Sector> Sectors = new List<Sector>();
        public List<Vertex> Vertices = new List<Vertex>();

        private Map()
        {
        }

        public Map? From(MapEntryCollection mapEntryCollection)
        {
            Map map = new Map();

            if (!MapEntryReader.ReadInto(mapEntryCollection, map))
                return null;

            return map;
        }
    }
}
