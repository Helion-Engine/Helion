using Helion.BSP.Geometry;
using Helion.Util;
using System.Collections.Generic;

namespace Helion.BSP.Node
{
    public class BspNode
    {
        public BspNode Left;
        public BspNode Right;
        public BspSegment Splitter;
        public List<SubsectorEdge> ClockwiseEdges;

        public bool IsParent => Left != null;
        public bool IsSubsector => ClockwiseEdges.Count > 0;
        public bool Degenerate => !IsParent && !IsSubsector;

        public BspNode(BspNode left, BspNode right, BspSegment splitter)
        {
            Left = left;
            Right = right;
            Splitter = splitter;
        }

        public BspNode(List<SubsectorEdge> edges)
        {
            Assert.Precondition(edges.Count >= 3, "Cannot create a child that is not at least a triangle");

            ClockwiseEdges = edges;
        }

        // TODO
    }
}
