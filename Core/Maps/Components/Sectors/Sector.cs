using Helion.Util;

namespace Helion.Maps.Components.Sectors
{
    public record Sector
    {
        /// <summary>
        /// The index of the index in the sectors entry.
        /// </summary>
        public int Index { get; init; }

        /// <summary>
        /// The Z height of the floor.
        /// </summary>
        public short FloorZ { get; init; }

        /// <summary>
        /// The Z height of the ceiling.
        /// </summary>
        public short CeilingZ { get; init; }

        /// <summary>
        /// The texture for the floor.
        /// </summary>
        public CIString FloorTexture { get; init; }

        /// <summary>
        /// The texture for the ceiling.
        /// </summary>
        public CIString CeilingTexture { get; init; }

        /// <summary>
        /// The light level. This does not need to be between 0 - 256, it
        /// supports the full range (and some maps use this for light tricks).
        /// </summary>
        public short LightLevel { get; init; }

        /// <summary>
        /// The tag lookup ID for the sector.
        /// </summary>
        public ushort Tag { get; init; }
    }
}
