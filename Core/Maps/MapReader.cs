using Helion.Maps.Doom;

namespace Helion.Maps
{
    /// <summary>
    /// A helper class for reading maps.
    /// </summary>
    public static class MapReader
    {
        /// <summary>
        /// Reads a collection of map entries into a map.
        /// </summary>
        /// <param name="map">The map to read.</param>
        /// <returns>A processed map, or null if the map data is corrupt or
        /// missing critical elements.</returns>
        public static IMap? Read(MapEntryCollection map)
        {
            if (map.IsDoomMap)
                return DoomMap.Create(map);
            
            // TODO: Hexen here.
            if (map.IsHexenMap)
                return null;
            
            // TODO: UDMF here.
            return null;
        }
    }
}