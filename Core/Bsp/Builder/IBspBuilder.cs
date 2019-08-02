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
        /// <remarks>
        /// Failure will only happen if the map is very corrupt beyond any hope
        /// of repair, or if the entire map is degenerate (and therefore is not
        /// playable).
        /// </remarks>
        /// <returns>The tree that was built, or null if there was a building
        /// error.</returns>
        BspNode? Build();
    }
}