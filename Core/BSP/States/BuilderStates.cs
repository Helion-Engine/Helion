namespace Helion.BSP.States
{
    public enum BuilderState
    {
        NotStarted,
        CheckingConvexity,
        CreatingLeafNode,
        FindingSplitter,
        PartitioningSegments,
        GeneratingMinisegs,
        FinishingSplit,
        Complete
    }

    public class BuilderStates
    {
        public BuilderState Previous = BuilderState.NotStarted;
        public BuilderState Current = BuilderState.NotStarted;

        public void SetState(BuilderState state)
        {
            Previous = Current;
            Current = state;
        }
    }
}
