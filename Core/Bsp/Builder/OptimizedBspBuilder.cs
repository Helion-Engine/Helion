using Helion.Bsp.Node;
using Helion.Maps;

namespace Helion.Bsp.Builder
{
    /// <summary>
    /// An optimized version of the BSP builder that should be used for when we
    /// are not debugging (as in used in map building, world building, etc).
    /// </summary>
    public class OptimizedBspBuilderBase : BspBuilderBase
    {
        public OptimizedBspBuilderBase(Map map) : this(map, new BspConfig())
        {
        }

        public OptimizedBspBuilderBase(Map map, BspConfig config) :
            base(map, config)
        {
            // TODO: We need to make the optimized components and set them.
        }

        /// <summary>
        /// Builds the entire tree and returns the root node upon completion.
        /// </summary>
        /// <returns>The root node of the built tree.</returns>
        public override BspNode? Build()
        {
            if (!Done)
            {
                while (!Done)
                    Execute();
                Root.StripDegenerateNodes();
            }
            
            return Root.IsDegenerate ? null : Root;
        }
    }
}
