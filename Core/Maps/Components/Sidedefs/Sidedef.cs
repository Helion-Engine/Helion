using Helion.Maps.Components.Sectors;
using Helion.Util;
using Helion.Util.Geometry.Vectors;

namespace Helion.Maps.Components.Sidedefs
{
    public record Sidedef
    {
        /// <summary>
        /// The texture offsets.
        /// </summary>
        public Vec2I Offset { get; init; }

        /// <summary>
        /// The upper texture name.
        /// </summary>
        public CIString UpperTexture { get; init; }

        /// <summary>
        /// The middle texture name.
        /// </summary>
        public CIString MiddleTexture { get; init; }

        /// <summary>
        /// The lower texture name.
        /// </summary>
        public CIString LowerTexture { get; init; }

        /// <summary>
        /// Gets the sector this side references.
        /// </summary>
        public Sector Sector { get; init; }
    }
}
