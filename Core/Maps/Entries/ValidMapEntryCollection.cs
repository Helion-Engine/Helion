using Helion.Maps.MapStructures;
using Helion.Util;
using System.IO;
using static Helion.Util.Assert;

namespace Helion.Maps
{
    // TODO: Can we convert any of these readers to `unsafe` to speed it up?
    // It would probably be a lot faster by just using pointers into the data
    // structures instead of making a byte reader do a bunch of conversions.

    /// <summary>
    /// A map entry collection where every instance holds an invariant that it
    /// can be read safely without needing to check any of its data structures.
    /// </summary>
    public class ValidMapEntryCollection : MapEntryCollection
    {
        private ValidMapEntryCollection(MapEntryCollection map) : base(map.Name, map.Vertices,
            map.Sectors, map.Sidedefs, map.Linedefs, map.Segments, map.Subsectors, map.Nodes,
            map.Things, map.Blockmap, map.Reject, map.Scripts, map.Behavior, map.Dialogue,
            map.Textmap, map.Znodes, map.Endmap)
        {
            Postcondition(HasRequiredComponents(this), $"Error when copy constructing a valid map entry collection");
            Postcondition(HasValidDataStructures(this), $"Trying to make a vlaid map entry collection from invalid data");
        }

        public static bool VerticesValid(MapEntryCollection map)
        {
            return map.Vertices != null && (map.Vertices.Length % Vertex.Bytes == 0);
        }

        public static bool SectorsValid(MapEntryCollection map)
        {
            return map.Sectors != null && (map.Sectors.Length % Sector.Bytes == 0);
        }

        public static bool SidedefsValid(MapEntryCollection map)
        {
            if (map.Sectors == null || map.Sidedefs == null)
                return false;

            int sectorCount = map.Sectors.Length / Sector.Bytes;

            if (map.Sidedefs.Length % Sidedef.Bytes != 0)
                return false;

            BinaryReader sidedefReader = new ByteReader(map.Sidedefs);
            for (int i = 0; i < map.Sidedefs.Length / Sidedef.Bytes; i++)
            {
                sidedefReader.ReadBytes(28); // Skip until the sector index.
                if (sidedefReader.ReadInt16() >= sectorCount)
                    return false;
            }

            return true;
        }

        public static bool DoomLinedefsValid(MapEntryCollection map)
        {
            // TODO
            return true;
        }

        public static bool HexenLinedefsValid(MapEntryCollection map)
        {
            // TODO
            return true;
        }

        public static bool SegmentsValid(MapEntryCollection map)
        {
            // TODO
            return true;
        }

        public static bool SubsectorsValid(MapEntryCollection map)
        {
            // TODO
            return true;
        }

        public static bool NodesValid(MapEntryCollection map)
        {
            // TODO
            return true;
        }

        public static bool DoomThingsValid(MapEntryCollection map)
        {
            // TODO
            return true;
        }

        public static bool HexenThingsValid(MapEntryCollection map)
        {
            // TODO
            return true;
        }

        private static bool HasRequiredComponents(MapEntryCollection map)
        {
            return !map.Name.Empty() && (map.IsUDMFMap || map.IsHexenMap || map.IsDoomMap);
        }

        private static bool HasValidVanillaSharedDataStructures(MapEntryCollection map)
        {
            return VerticesValid(map) && SectorsValid(map) && SidedefsValid(map) &&
                   SegmentsValid(map) && SubsectorsValid(map) && NodesValid(map);
        }

        /// <summary>
        /// Checks that all the elements of the map are valid (implying they
        /// can be read/indexed without needing to do bounds checking or any
        /// other sanity checks).
        /// </summary>
        /// <param name="map">The map with components to check.</param>
        /// <returns>True if the data for the entries are valid, false if not.</returns>
        private static bool HasValidDataStructures(MapEntryCollection map)
        {
            switch (map.MapType)
            {
            case MapType.Doom:
            default:
                return HasValidVanillaSharedDataStructures(map) && DoomLinedefsValid(map) && DoomThingsValid(map);
            case MapType.Hexen:
                return HasValidVanillaSharedDataStructures(map) && HexenLinedefsValid(map) && HexenThingsValid(map);
            case MapType.UDMF:
                Fail("UDMF not currently supported");
                return false;
            }
        }

        /// <summary>
        /// Creates a valid map entry collection from an existing map, if it is
        /// valid. If not, an empty value is returned.
        /// </summary>
        /// <param name="map">The map to try to make a valid map entry 
        /// collection from.</param>
        /// <returns>A valid object on success, an empty optional on failure.
        /// </returns>
        public static ValidMapEntryCollection? From(MapEntryCollection map)
        {
            if (!HasRequiredComponents(map) || !HasValidDataStructures(map))
                return null;
            return new ValidMapEntryCollection(map);
        }
    }
}
