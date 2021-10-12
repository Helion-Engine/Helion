using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Maps.Components;

namespace Helion.Maps.Doom.Components;

public class DoomNode : INode
{
    public const uint IsSubsectorMask = 0x00008000;

    public Seg2D Splitter { get; }
    public Box2D RightBoundingBox { get; }
    public Box2D LeftBoundingBox { get; }
    public uint LeftChild { get; }
    public uint RightChild { get; }
    public bool LeftIsSubsector { get; }
    public bool RightIsSubsector { get; }

    public DoomNode(Seg2D splitter, Box2D rightBox, Box2D leftBox, uint leftChild, uint rightChild)
    {
        Splitter = splitter;
        RightBoundingBox = rightBox;
        LeftBoundingBox = leftBox;
        LeftChild = leftChild;
        RightChild = rightChild;
        LeftIsSubsector = (leftChild & IsSubsectorMask) == IsSubsectorMask;
        RightIsSubsector = (rightChild & IsSubsectorMask) == IsSubsectorMask;
    }
}
