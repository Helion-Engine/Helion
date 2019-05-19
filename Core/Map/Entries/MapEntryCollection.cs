using Helion.Util;

namespace Helion.Map
{
    /// <summary>
    /// Contains all the entries that belong to a map collection. This is *not*
    /// intended to be a valid set of data, but a temporary one that is being
    /// built. This should never be passed around as this type, but instead you
    /// should try to convert it into a ValidMapEntryCollection when you will
    /// no longer be adding components.
    /// </summary>
    public class MapEntryCollection
    {
        /// <summary>
        /// The name of the map.
        /// </summary>
        public UpperString Name = "";

        // The following are a list of all the components that may be present.
        public byte[]? Vertices = null;
        public byte[]? Sectors = null;
        public byte[]? Sidedefs = null;
        public byte[]? Linedefs = null;
        public byte[]? Segments = null;
        public byte[]? Subsectors = null;
        public byte[]? Nodes = null;
        public byte[]? Things = null;
        public byte[]? Blockmap = null;
        public byte[]? Reject = null;
        public byte[]? Scripts = null;
        public byte[]? Behavior = null;
        public byte[]? Dialogue = null;
        public byte[]? Textmap = null;
        public byte[]? Znodes = null;
        public byte[]? Endmap = null;

        /// <summary>
        /// Creates a blank collection with no components or name set.
        /// </summary>
        public MapEntryCollection() { }

        /// <summary>
        /// Sets all the fields for the map entry collection in one go.
        /// </summary>
        /// <remarks>
        /// This is primarily used by the <see cref="ValidMapEntryCollection"/>
        /// child class to reduce code duplication and make it easier to spot
        /// any errors if we add any field to this collection in the future.
        /// </remarks>
        /// <param name="name">The name of the map.</param>
        /// <param name="vertices">The entry for this type.</param>
        /// <param name="sectors">The entry for this type.</param>
        /// <param name="sidedefs">The entry for this type.</param>
        /// <param name="linedefs">The entry for this type.</param>
        /// <param name="segments">The entry for this type.</param>
        /// <param name="subsectors">The entry for this type.</param>
        /// <param name="nodes">The entry for this type.</param>
        /// <param name="things">The entry for this type.</param>
        /// <param name="blockmap">The entry for this type.</param>
        /// <param name="reject">The entry for this type.</param>
        /// <param name="scripts">The entry for this type.</param>
        /// <param name="behavior">The entry for this type.</param>
        /// <param name="dialogue">The entry for this type.</param>
        /// <param name="textmap">The entry for this type.</param>
        /// <param name="znodes">The entry for this type.</param>
        /// <param name="endmap">The entry for this type.</param>
        public MapEntryCollection(UpperString name, byte[]? vertices, byte[]? sectors,
            byte[]? sidedefs, byte[]? linedefs, byte[]? segments, byte[]? subsectors, byte[]? nodes,
            byte[]? things, byte[]? blockmap, byte[]? reject, byte[]? scripts, byte[]? behavior, 
            byte[]? dialogue, byte[]? textmap, byte[]? znodes, byte[]? endmap)
        {
            Name = name;
            Vertices = vertices;
            Sectors = sectors;
            Sidedefs = sidedefs;
            Linedefs = linedefs;
            Segments = segments;
            Subsectors = subsectors;
            Nodes = nodes;
            Things = things;
            Blockmap = blockmap;
            Reject = reject;
            Scripts = scripts;
            Behavior = behavior;
            Dialogue = dialogue;
            Textmap = textmap;
            Znodes = znodes;
            Endmap = endmap;
        }

        /// <summary>
        /// Gets the map type for this collection. The value it returns may be 
        /// invalid so you will have to check against the IsValid property.
        /// </summary>
        public MapType MapType => IsUDMFMap ? MapType.UDMF : (IsHexenMap ? MapType.Hexen : MapType.Doom);

        /// <summary>
        /// Checks if this is a well formed map entry collection that is 
        /// eligible to be converted into a valid map entry collection.
        /// </summary>
        /// <returns>True if it's got the required components as per map type,
        /// false otherwise.</returns>
        public bool IsValid()
        {
            if (Name.Empty())
                return false;

            switch (MapType)
            {
            case MapType.Doom:
                return IsDoomMap;
            case MapType.Hexen:
                return IsHexenMap;
            case MapType.UDMF:
                return IsUDMFMap;
            }

            return false;
        }

        /// <summary>
        /// Checks if this is a doom map.
        /// </summary>
        public bool IsDoomMap => Vertices != null && Sectors != null && Sidedefs != null && Linedefs != null && Things != null;

        /// <summary>
        /// Checks if this is a hexen map.
        /// </summary>
        public bool IsHexenMap => IsDoomMap && Behavior != null;

        /// <summary>
        /// Checks if this is a text map.
        /// </summary>
        public bool IsUDMFMap => Textmap != null;
    }
}
