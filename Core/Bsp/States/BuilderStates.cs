namespace Helion.Bsp.States
{
    /// <summary>
    /// All the states the BSP builder can be in.
    /// </summary>
    public enum BuilderState
    {
        NotStarted,
        CheckingConvexity,
        CreatingLeafNode,
        FindingSplitter,
        PartitioningSegments,
        GeneratingMinisegs,
        FinishingSplit,
        Complete,
    }

    /// <summary>
    /// A simple container of the current BSP state.
    /// </summary>
    public class BuilderStates
    {
        public BuilderState Previous = BuilderState.NotStarted;
        public BuilderState Current = BuilderState.NotStarted;

        /// <summary>
        /// Sets it to the state provided, and sets the previous state to be
        /// the current.
        /// </summary>
        /// <param name="state">The new state to set it to.</param>
        public void SetState(BuilderState state)
        {
            Previous = Current;
            Current = state;
        }
    }
}
