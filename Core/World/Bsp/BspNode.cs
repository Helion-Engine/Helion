using Helion.Util.Geometry;

namespace Helion.World.Bsp
{
    /// <summary>
    /// A cache aware BSP node that uses as little space as possible.
    /// </summary>
    public readonly struct BspNodeCompact
    {
        /// <summary>
        /// The bit that is set in each child to indicate whether it is a node
        /// or a subsector.
        /// </summary>
        public const uint IsSubsectorBit = 0x80000000U;

        /// <summary>
        /// The mask that is used for grabbing the lower 15 bits.
        /// </summary>
        public const uint SubsectorMask = 0x7FFFFFFFU;

        /// <summary>
        /// A left child index, which is either an index to a subsector, or a 
        /// child node depending on whether <see cref="IsSubsectorBit"/> is 
        /// set.
        /// </summary>
        public readonly uint LeftChild;

        /// <summary>
        /// A right child index, which is either an index to a subsector, or a
        /// child node depending on whether <see cref="IsSubsectorBit"/> is 
        /// set.
        /// </summary>
        public readonly uint RightChild;

        /// <summary>
        /// The splitter that made this line, which is also used for finding
        /// out which side of the line a point is on.
        /// </summary>
        // TODO: Do we want a customized BspSplitter type that is a struct as well?
        public readonly Seg2D Splitter;

        /// <summary>
        /// The bounding box of this node.
        /// </summary>
        public readonly Box2D BoundingBox;

        /// <summary>
        /// True if the left child field is a subsector or not.
        /// </summary>
        public bool IsLeftSubsector => (LeftChild & IsSubsectorBit) == IsSubsectorBit;

        /// <summary>
        /// True if the right child field is a subsector not.
        /// </summary>
        public bool IsRightSubsector => (RightChild & IsSubsectorBit) == IsSubsectorBit;

        /// <summary>
        /// Gets the left childs index as if it were a subsector (without the
        /// subsector bit set).
        /// </summary>
        public uint LeftChildAsSubsector => LeftChild & SubsectorMask;

        /// <summary>
        /// Gets the right childs index as if it were a subsector (without the
        /// subsector bit set).
        /// </summary>
        public uint RightChildAsSubsector => RightChild & SubsectorMask;

        /// <summary>
        /// Creates a compact node from a left and right index.
        /// </summary>
        /// <param name="leftChild">The left child index.</param>
        /// <param name="rightChild">The right child index.</param>
        /// <param name="splitter">The segment that split the node to create
        /// the children.</param>
        /// <param name="boundingBox">The bounding box for this node, which is 
        /// the minimal size needed to contain every child under this.</param>
        public BspNodeCompact(uint leftChild, uint rightChild, Seg2D splitter, Box2D boundingBox)
        {
            LeftChild = leftChild;
            RightChild = rightChild;
            Splitter = splitter;
            BoundingBox = boundingBox;
        }

        /// <summary>
        /// Checks if the index is a subsector.
        /// </summary>
        /// <param name="index">The subsector index.</param>
        /// <returns>True if it's a subsector index, false if it's a node 
        /// index.</returns>
        public static bool IsSubsectorIndex(ushort index) => (index & IsSubsectorBit) > 0;
    }
}
