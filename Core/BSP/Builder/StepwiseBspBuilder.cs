using Helion.BSP.Node;
using Helion.BSP.States;
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
            if (Done)
                return;

            BuilderState originalState = States.Current;
            BuilderState currentState = States.Current;
            while (originalState == currentState)
            {
                ExecuteMinorStep();
                currentState = States.Current;
            }
        }

        public void ExecuteMinorStep()
        {
            if (Done)
                return;

            Execute();
        }

        public BspNode GetTree() => Root;
        public BspWorkItem? GetCurrentWorkItem() => WorkItems.TryPeek(out BspWorkItem result) ? result : null;
    }
}
