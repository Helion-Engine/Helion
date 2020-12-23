using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Segments;
using static Helion.Maps.Components.GL.GLComponents;

namespace Helion.Maps.Components.GL
{
    public class GLNode
    {
        public readonly Seg2D Splitter;
        public readonly Box2D RightBox;
        public readonly Box2D LeftBox;
        public readonly uint RightChild;
        public readonly bool IsRightSubsector;
        public readonly uint LeftChild;
        public readonly bool IsLeftSubsector;

        private GLNode(Seg2D splitter, Box2D rightBox, Box2D leftBox, uint rightChild, bool isRightSubsector,
            uint leftChild, bool isLeftSubsector)
        {
            Splitter = splitter;
            RightBox = rightBox;
            LeftBox = leftBox;
            RightChild = rightChild;
            IsRightSubsector = isRightSubsector;
            LeftChild = leftChild;
            IsLeftSubsector = isLeftSubsector;
        }

        public static GLNode FromV2(Seg2D splitter, Box2D rightBox, Box2D leftBox, uint rightChild, uint leftChild)
        {
            return new(
                splitter,
                rightBox,
                leftBox,
                rightChild & ~NodeIsSubsectorV2,
                (rightChild & NodeIsSubsectorV2) == NodeIsSubsectorV2,
                leftChild & ~NodeIsSubsectorV2,
                (leftChild & NodeIsSubsectorV2) == NodeIsSubsectorV2
            );
        }

        public static GLNode FromV5(Seg2D splitter, Box2D rightBox, Box2D leftBox, uint rightChild, uint leftChild)
        {
            return new(
                splitter,
                rightBox,
                leftBox,
                rightChild & ~NodeIsSubsectorV5,
                (rightChild & NodeIsSubsectorV5) == NodeIsSubsectorV5,
                leftChild & ~NodeIsSubsectorV5,
                (leftChild & NodeIsSubsectorV5) == NodeIsSubsectorV5
            );
        }
    }
}
