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

        public void ExecuteFullCycleStep()
        {
            if (Done)
                return;

            if (States.Current == BuilderState.CheckingConvexity)
                ExecuteMajorStep();

            while (States.Current != BuilderState.CheckingConvexity && !Done)
                ExecuteMajorStep();
        }

        /// <summary>
        /// Advances to the next major state.
        /// </summary>
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

        /// <summary>
        /// Advances the smallest atomic unit possible.
        /// </summary>
        public void ExecuteMinorStep()
        {
            if (Done)
                return;

            Execute();
        }

        /// <summary>
        /// Moves until either the current work item is the branch provided, or
        /// the building is done.
        /// </summary>
        /// <param name="branch">The branch path to go to.</param>
        public void ExecuteUntilBranch(string branch)
        {
            string upperBranch = branch.ToUpper();
            while (!Done)
            {
                BspWorkItem? item = GetCurrentWorkItem();
                if (item != null)
                {
                    if (item.BranchPath == upperBranch)
                        break;
                    ExecuteFullCycleStep();
                }
            }
        }

        public BspNode GetTree() => Root;
        public BspWorkItem? GetCurrentWorkItem() => WorkItems.TryPeek(out BspWorkItem result) ? result : null;
    }
}
