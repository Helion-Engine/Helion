using Helion.Util.Geometry.Vectors;

namespace Helion.BspOld.Geometry
{
    /// <summary>
    /// Indicates a line can be used for BSP actions within the BSP builder.
    /// </summary>
    public interface IBspUsableLine
    {
        /// <summary>
        /// The starting vertex of the line.
        /// </summary>
        Vec2D StartPosition { get; }
        
        /// <summary>
        /// The ending vertex of the line.
        /// </summary>
        Vec2D EndPosition { get; }
        
        /// <summary>
        /// If the line is one-sided or not.
        /// </summary>
        bool OneSided { get; }
    }
}