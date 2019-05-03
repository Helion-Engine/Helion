using Helion.Map;

namespace Helion.BSP.Builder
{
    public class StepwiseBspBuilder : BspBuilder
    {
        protected StepwiseBspBuilder(ValidMapEntryCollection map) : this(map, new BspConfig())
        {
        }

        protected StepwiseBspBuilder(ValidMapEntryCollection map, BspConfig config) :
            base(map, config)
        {
        }
    }
}
