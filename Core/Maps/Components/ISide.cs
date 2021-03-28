using Helion.Geometry.Vectors;
using Helion.Maps.Specials;

namespace Helion.Maps.Components
{
    /// <summary>
    /// A side of a line.
    /// </summary>
    public interface ISide
    {
        /// <summary>
        /// A unique ID for the side.
        /// </summary>
        int Id { get; }
        
        /// <summary>
        /// The texture offsets.
        /// </summary>
        Vec2I Offset { get; }
        
        /// <summary>
        /// The upper texture name.
        /// </summary>
        string UpperTexture { get; }
        
        /// <summary>
        /// The middle texture name.
        /// </summary>
        string MiddleTexture { get; }
        
        /// <summary>
        /// The lower texture name.
        /// </summary>
        string LowerTexture { get; }
        
        /// <summary>
        /// Gets the sector this side references.
        /// </summary>
        /// <returns>The sector this side references.</returns>
        ISector GetSector();

        public SideScrollData? ScrollData { get; set; }
    }
}