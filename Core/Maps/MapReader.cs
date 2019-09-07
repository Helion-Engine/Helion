using System;
using Helion.Maps.Doom;
using Helion.Maps.Hexen;

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
            switch (map.MapType)
            {
            case MapType.Doom:
                return DoomMap.Create(map);
            case MapType.Hexen:
                return HexenMap.Create(map);
            default:
                // TODO: UDMF!
                return null;
            }
        }
    }
}