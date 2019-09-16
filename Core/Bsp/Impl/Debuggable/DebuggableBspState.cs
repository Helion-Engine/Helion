namespace Helion.Bsp.Impl.Debuggable
{
    /// <summary>
    /// All the states the BSP builder can be in.
    /// </summary>
    public enum DebuggableBspState
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