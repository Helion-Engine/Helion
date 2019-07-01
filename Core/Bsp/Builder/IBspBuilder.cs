using Helion.Bsp.Node;

namespace Helion.Bsp.Builder
{
    /// <summary>
    /// Represents a BSP tree builder that can be used to create BSP trees for
    /// rendering, physics, and collision detection.
    /// </summary>
    public interface IBspBuilder
    {
        /// <summary>
        /// Builds the entire tree to completion.
        /// </summary>
        /// <returns>The tree that was built.</returns>
        BspNode? Build();
    }
}