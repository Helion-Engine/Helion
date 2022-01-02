using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using System.Runtime.InteropServices;

namespace Helion.World.Bsp;

/// <summary>
/// A cache aware BSP node that uses as little space as possible.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct BspNodeCompact
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
    /// The splitter that made this line, which is also used for finding
    /// out which side of the line a point is on.
    /// </summary>
    public Seg2D Splitter;

    /// <summary>
    /// The bounding box of this node.
    /// </summary>
    public Box2D BoundingBox;

    public fixed uint Children[2];

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
        Splitter = splitter;
        BoundingBox = boundingBox;
        Children[0] = leftChild;
        Children[1] = rightChild;
    }
}
