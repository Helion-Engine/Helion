using Helion.BSP.Node;
using Helion.BSP.States;
using Helion.Maps;

namespace Helion.BSP.Builder
{
    /// <summary>
    /// An implementation of the BSP builder which uses debuggable/steppable
    /// executions so we can easily see what is happening at each step.
    /// </summary>
    public class StepwiseBspBuilder : BspBuilder
    {
        public StepwiseBspBuilder(ValidMapEntryCollection map) : this(map, new BspConfig())
        {
        }

        public StepwiseBspBuilder(ValidMapEntryCollection map, BspConfig config) :
            base(map, config)
        {
        }

        /// <summary>
        /// Steps through all major states until it reaches the convexity check
        /// or it completes.
        /// </summary>
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

        /// <summary>
        /// Gets the root node of the tree.
        /// </summary>
        /// <returns>The root node of the tree that is being built.</returns>
        public BspNode GetTree() => Root;

        /// <summary>
        /// Gets the current work item.
        /// </summary>
        /// <returns>The current work item, if any.</returns>
        public BspWorkItem? GetCurrentWorkItem() => WorkItems.TryPeek(out BspWorkItem result) ? result : null;
    }
}
