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
        public BuilderState Next = BuilderState.NotStarted;
    }
}
