namespace Helion.BspOld.States
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
}