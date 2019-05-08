using Helion.BSP.Node;
using Helion.Map;

namespace Helion.BSP.Builder
{
    public class OptimizedBspBuilder : BspBuilder
    {
        protected OptimizedBspBuilder(ValidMapEntryCollection map) : this(map, new BspConfig())
        {
        }

        protected OptimizedBspBuilder(ValidMapEntryCollection map, BspConfig config) :
            base(map, config)
        {
        }

        public BspNode Build()
        {
            // TODO
            return root;
        }
    }
}
