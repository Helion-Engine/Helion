using Helion.BSP.Node;
using Helion.Map;

namespace Helion.BSP.Builder
{
    public class StepwiseBspBuilder : BspBuilder
    {
        public StepwiseBspBuilder(ValidMapEntryCollection map) : this(map, new BspConfig())
        {
        }

        public StepwiseBspBuilder(ValidMapEntryCollection map, BspConfig config) :
            base(map, config)
        {
        }

        public void ExecuteMajorStep()
        {
            // TODO: Implement later to loop until the state changes.
            ExecuteMinorStep();
        }

        public void ExecuteMinorStep()
        {
            if (Done)
                return;
            Execute();
        }

        public BspNode GetTree() => Root;
    }
}
