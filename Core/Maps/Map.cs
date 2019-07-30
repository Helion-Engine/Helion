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
    public class Map : IMap
    {
        /// <inheritdoc/>
        public CIString Name { get; }
        
        /// <inheritdoc/>
        public MapType MapType { get; }
        
        /// <inheritdoc/>
        public IList<Line> Lines { get; } = new List<Line>();
        
        /// <inheritdoc/>
        public IList<Thing> Things { get; } = new List<Thing>();
        
        /// <inheritdoc/>
        public IList<Side> Sides { get; } = new List<Side>();
        
        /// <inheritdoc/>
        public IList<Sector> Sectors { get; } = new List<Sector>();
        
        /// <inheritdoc/>
        public IList<SectorFlat> SectorFlats { get; } = new List<SectorFlat>();
        
        /// <inheritdoc/>
        public IList<Vertex> Vertices { get; } = new List<Vertex>();

        private Map(CIString name, MapType type)
        {
            Name = name;
            MapType = type;
        }

        /// <summary>
        /// Creates a map from a collection of map entries.
        /// </summary>
        /// <param name="mapEntryCollection">The map entry collection.</param>
        /// <returns>The map on reading, or null if there was any errors when
        /// processing the map.</returns>
        public static Map? From(MapEntryCollection mapEntryCollection)
        {
            Map map = new Map(mapEntryCollection.Name, mapEntryCollection.MapType);
            return MapEntryReader.ReadInto(mapEntryCollection, map) ? map : null;
        }
    }
}