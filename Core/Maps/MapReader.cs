using Helion.Maps.Doom;
using Helion.Maps.Hexen;
using Helion.Resources.Archives;
using Helion.Resources.Definitions.Compatibility;

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
        /// <param name="archive">The archive.</param>
        /// <param name="map">The map to read.</param>
        /// <param name="compatibility">The compatibility definition, if any.
        /// This can be null. If present, it will mutate the resulting map.
        /// </param>
        /// <returns>A processed map, or null if the map data is corrupt or
        /// missing critical elements.</returns>
        public static IMap? Read(Archive archive, MapEntryCollection map, CompatibilityMapDefinition? compatibility = null)
        {
            switch (map.MapType)
            {
            case MapType.Doom:
                return DoomMap.Create(archive, map, compatibility);
            case MapType.Hexen:
                return HexenMap.Create(archive, map, compatibility);
            default:
                // TODO: UDMF!
                return null;
            }
        }
    }
}