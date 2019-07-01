using Helion.Util.Geometry;

namespace Helion.Bsp.Builder.GLBSP
{
    public struct GLNode
    {
        public readonly Seg2D Splitter;
        public readonly Box2D RightBox;
        public readonly Box2D LeftBox;
        public readonly ushort RightChild;
        public readonly ushort LeftChild;

        public GLNode(Seg2D splitter, Box2D rightBox, Box2D leftBox, ushort rightChild, ushort leftChild)
        {
            Splitter = splitter;
            RightBox = rightBox;
            LeftBox = leftBox;
            RightChild = rightChild;
            LeftChild = leftChild;
        }
    }
}