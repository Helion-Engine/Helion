namespace Helion.World.Bsp;

/// <summary>
/// A helper class for propagating recursive information when building.
/// </summary>
public readonly struct BspCreateResultCompact
{
    /// <summary>
    /// If this result is for a subsector (if true), or a node (if false).
    /// </summary>
    public readonly bool IsSubsector;

    /// <summary>
    /// The index into either <see cref="CompactBspTree.Segments"/> or
    /// <see cref="CompactBspTree.Nodes"/> for the component.
    /// </summary>
    public readonly uint Index;

    /// <summary>
    /// Gets the index with the appropriate bit set if needed. This works
    /// for either the node or the subsector.
    /// </summary>
    public uint IndexWithBit => IsSubsector ? (Index | BspNodeCompact.IsSubsectorBit) : Index;

    private BspCreateResultCompact(bool isSubsector, uint index)
    {
        IsSubsector = isSubsector;
        Index = index;
    }

    /// <summary>
    /// Creates a result from a subsector index.
    /// </summary>
    /// <param name="index">The subsector index.</param>
    /// <returns>A result with the subsector index.</returns>
    public static BspCreateResultCompact Subsector(uint index) => new BspCreateResultCompact(true, index);

    /// <summary>
    /// Creates a result from a node index.
    /// </summary>
    /// <param name="index">The node index.</param>
    /// <returns>A result with the node index.</returns>
    public static BspCreateResultCompact Node(uint index) => new BspCreateResultCompact(false, index);
}
