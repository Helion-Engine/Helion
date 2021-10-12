using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;

namespace Helion.Maps.Components;

/// <summary>
/// Represents a node in a BSP tree for some map.
/// </summary>
public interface INode
{
    /// <summary>
    /// The splitter that partitions the BSP tree.
    /// </summary>
    Seg2D Splitter { get; }

    /// <summary>
    /// The right bounding box of this node.
    /// </summary>
    Box2D RightBoundingBox { get; }

    /// <summary>
    /// The right bounding box of this node.
    /// </summary>
    Box2D LeftBoundingBox { get; }

    /// <summary>
    /// The index of the left child (this may contain special bits to
    /// indicate whether it's a subsector not).
    /// </summary>
    uint LeftChild { get; }

    /// <summary>
    /// The index of the right child (this may contain special bits to
    /// indicate whether it's a subsector not).
    /// </summary>
    uint RightChild { get; }

    /// <summary>
    /// True if the left child is a subsector, false if it's a node.
    /// </summary>
    bool LeftIsSubsector { get; }

    /// <summary>
    /// True if the left child is a subsector, false if it's a node.
    /// </summary>
    bool RightIsSubsector { get; }
}

