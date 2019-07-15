namespace Helion.World.Geometry
{
    /// <summary>
    /// A helper class for propagating recursive information when building.
    /// </summary>
    public readonly struct BspCreateResult
    {
        /// <summary>
        /// If this result is for a subsector (if true), or a node (if false).
        /// </summary>
        public readonly bool IsSubsector;

        /// <summary>
        /// The index into either <see cref="BspTree.Segments"/> or
        /// <see cref="BspTree.Nodes"/> for the component.
        /// </summary>
        public readonly uint Index;

        /// <summary>
        /// Gets the index with the appropriate bit set if needed. This works 
        /// for either the node or the subsector.
        /// </summary>
        public uint IndexWithBit => IsSubsector ? (Index | BspNodeCompact.IsSubsectorBit) : Index;

        private BspCreateResult(bool isSubsector, uint index)
        {
            IsSubsector = isSubsector;
            Index = index;
        }

        /// <summary>
        /// Creates a result from a subsector index.
        /// </summary>
        /// <param name="index">The subsector index.</param>
        /// <returns>A result with the subsector index.</returns>
        public static BspCreateResult Subsector(uint index) => new BspCreateResult(true, index);

        /// <summary>
        /// Creates a result from a node index.
        /// </summary>
        /// <param name="index">The node index.</param>
        /// <returns>A result with the node index.</returns>
        public static BspCreateResult Node(uint index) => new BspCreateResult(false, index);
    }
}