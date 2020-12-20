using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Segments;

namespace Helion.Worlds.Bsp
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
        public const uint SubsectorBit = 0x80000000U;

        /// <summary>
        /// A left child index, which is either an index to a subsector, or a
        /// child node depending on whether <see cref="SubsectorBit"/> is
        /// set.
        /// </summary>
        public readonly uint LeftChild;

        /// <summary>
        /// A right child index, which is either an index to a subsector, or a
        /// child node depending on whether <see cref="SubsectorBit"/> is
        /// set.
        /// </summary>
        public readonly uint RightChild;

        /// <summary>
        /// The splitter that made this line, which is also used for finding
        /// out which side of the line a point is on.
        /// </summary>
        public readonly Seg2D Splitter;

        /// <summary>
        /// The bounding box of this node.
        /// </summary>
        public readonly Box2D BoundingBox;

        /// <summary>
        /// True if the left child field is a subsector or not.
        /// </summary>
        public bool IsLeftSubsector => (LeftChild & SubsectorBit) == SubsectorBit;

        /// <summary>
        /// True if the right child field is a subsector not.
        /// </summary>
        public bool IsRightSubsector => (RightChild & SubsectorBit) == SubsectorBit;

        /// <summary>
        /// Gets the left child's index as if it were a subsector (without the
        /// subsector bit set).
        /// </summary>
        public uint LeftChildAsSubsector => LeftChild & ~SubsectorBit;

        /// <summary>
        /// Gets the right child's index as if it were a subsector (without the
        /// subsector bit set).
        /// </summary>
        public uint RightChildAsSubsector => RightChild & ~SubsectorBit;

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
    }
}