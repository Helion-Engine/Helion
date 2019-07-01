using Helion.Util;

namespace Helion.Maps
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
        public byte[]? GLMap = null;
        public byte[]? GLVertices = null;
        public byte[]? GLSegments = null;
        public byte[]? GLSubsectors = null;
        public byte[]? GLNodes = null;
        public byte[]? GLPVS = null;

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
