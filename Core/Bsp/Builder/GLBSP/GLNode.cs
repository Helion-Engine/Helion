using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;

namespace Helion.Bsp.Builder.GLBSP;

public struct GLNode
{
    public readonly Seg2D Splitter;
    public readonly Box2D RightBox;
    public readonly Box2D LeftBox;
    public readonly uint RightChild;
    public readonly uint LeftChild;

    public GLNode(Seg2D splitter, Box2D rightBox, Box2D leftBox, uint rightChild, uint leftChild)
    {
        Splitter = splitter;
        RightBox = rightBox;
        LeftBox = leftBox;
        RightChild = rightChild;
        LeftChild = leftChild;
    }
}

