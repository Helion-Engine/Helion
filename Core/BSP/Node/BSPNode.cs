using Helion.BSP.Geometry;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.BSP.Node
{
    public class BspNode
    {
        public BspNode Left;
        public BspNode Right;
        public BspSegment Splitter;
        public IList<SubsectorEdge> ClockwiseEdges;

        public bool IsParent => Left != null;
        public bool IsSubsector => ClockwiseEdges.Count > 0;
        public bool Degenerate => !IsParent && !IsSubsector;

        public BspNode()
        {
        }

        public BspNode(BspNode left, BspNode right, BspSegment splitter)
        {
            Left = left;
            Right = right;
            Splitter = splitter;
        }

        public BspNode(IList<SubsectorEdge> edges)
        {
            Precondition(edges.Count >= 3, "Cannot create a child that is not at least a triangle");

            ClockwiseEdges = edges;
        }

        public int CalculateSubsectorCount()
        {
            if (IsParent)
                return Left.CalculateSubsectorCount() + Right.CalculateSubsectorCount();
            else
                return IsSubsector ? 1 : 0;
        }

        public int CalculateParentNodeCount()
        {
            if (IsParent)
                return 1 + Left.CalculateSubsectorCount() + Right.CalculateSubsectorCount();
            else
                return 0;
        }

        public int CalculateTotalNodeCount()
        {
            int left = Left != null ? Left.CalculateTotalNodeCount() : 0;
            int right = Right != null ? Right.CalculateTotalNodeCount() : 0;
            return 1 + left + right;
        }
    }
}
