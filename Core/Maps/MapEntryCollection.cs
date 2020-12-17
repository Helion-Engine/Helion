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
        public CIString Name = string.Empty;

        // The following are a list of all the components that may be present.
        public byte[]? Vertices { get; set; }
        public byte[]? Sectors { get; set; }
        public byte[]? Sidedefs { get; set; }
        public byte[]? Linedefs { get; set; }
        public byte[]? Segments { get; set; }
        public byte[]? Subsectors { get; set; }
        public byte[]? Nodes { get; set; }
        public byte[]? Things { get; set; }
        public byte[]? Blockmap { get; set; }
        public byte[]? Reject { get; set; }
        public byte[]? Scripts { get; set; }
        public byte[]? Behavior { get; set; }
        public byte[]? Dialogue { get; set; }
        public byte[]? Textmap { get; set; }
        public byte[]? Znodes { get; set; }
        public byte[]? Endmap { get; set; }
        public byte[]? GLMap { get; set; }
        public byte[]? GLVertices { get; set; }
        public byte[]? GLSegments { get; set; }
        public byte[]? GLSubsectors { get; set; }
        public byte[]? GLNodes { get; set; }
        public byte[]? GLPVS { get; set; }

        /// <summary>
        /// Gets the map type for this collection. The value it returns may be
        /// invalid so you will have to check against the IsValid property.
        /// </summary>
        public MapType MapType => IsUDMFMap ? MapType.UDMF : (IsHexenMap ? MapType.Hexen : MapType.Doom);

        /// <summary>
        /// True if the map has all the required GL components, false if not.
        /// </summary>
        public bool HasAllGLComponents => GLVertices != null && GLSegments != null && GLSubsectors != null && GLNodes != null;

        /// <summary>
        /// Checks if this is a well formed map entry collection that is
        /// eligible to be converted into a valid map entry collection.
        /// </summary>
        /// <returns>True if it's got the required components as per map type,
        /// false otherwise.</returns>
        public bool IsValid()
        {
            if (Name.Empty)
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
