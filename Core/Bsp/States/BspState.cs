namespace Helion.Bsp.States;

/// <summary>
/// All the states the BSP builder can be in.
/// </summary>
public enum BspState
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

