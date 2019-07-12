using System.Collections.Generic;
using System.Linq;
using Helion.Bsp.Geometry;
using static Helion.Util.Assert;

namespace Helion.Bsp.Node
{
    /// <summary>
    /// A node in a BSP tree.
    /// </summary>
    public class BspNode
    {
        /// <summary>
        /// Creates a degenerate (or rather, empty) BSP node.
        /// </summary>
        public BspNode()
        {
        }

        /// <summary>
        /// Creates a parent node from some split.
        /// </summary>
        /// <param name="left">The left child.</param>
        /// <param name="right">The right child.</param>
        /// <param name="splitter">The splitter that divided the children.
        /// </param>
        public BspNode(BspNode left, BspNode right, BspSegment splitter)
        {
            Left = left;
            Right = right;
            Splitter = splitter;
        }

        /// <summary>
        /// Creates a leaf node (subsector) from a closed convex polygon of
        /// clockwise edges.
        /// </summary>
        /// <param name="edges">The clockwise edges for this subsector.</param>
        public BspNode(IList<SubsectorEdge> edges)
        {
            Precondition(edges.Count >= 3, "Cannot create a child that is not at least a triangle");
            Precondition(EdgesAreHeadToTail(edges), "BSP node subsector edges are not closed");

            ClockwiseEdges = edges;
        }

        /// <summary>
        /// The left node. This is null if its a degenerate or leaf node.
        /// </summary>
        public BspNode? Left { get; private set; }

        /// <summary>
        /// The right node. This is null if its a degenerate or leaf node.
        /// </summary>
        public BspNode? Right { get; private set; }

        /// <summary>
        /// The splitter that made up this node. This is only present if it
        /// is a parent node.
        /// </summary>
        public BspSegment? Splitter { get; internal set; }

        /// <summary>
        /// A list of all the clockwise edges. This is populated if and only if
        /// this is a subsector.
        /// </summary>
        public IList<SubsectorEdge> ClockwiseEdges { get; internal set; } = new List<SubsectorEdge>();

        /// <summary>
        /// True if it's a parent (has children), false otherwise.
        /// </summary>
        public bool IsParent => Left != null && Right != null;

        /// <summary>
        /// True if it is a subsector, false if not.
        /// </summary>
        public bool IsSubsector => ClockwiseEdges.Count > 0;

        /// <summary>
        /// True if it's a degenerate node due to a bad BSP map, false if not.
        /// </summary>
        public bool IsDegenerate => !IsParent && !IsSubsector;

        /// <summary>
        /// Recursively compresses all the degenerate nodes so that no nodes
        /// exist in the tree that are degenerate, unless every single node is
        /// degenerate.
        /// </summary>
        public void StripDegenerateNodes()
        {
            Left?.StripDegenerateNodes();
            Right?.StripDegenerateNodes();

            if (Left == null || Right == null)
                return;

            if (Left.IsDegenerate && !Right.IsDegenerate)
                HandleLeftDegenerateCase();
            else if (Right.IsDegenerate && !Left.IsDegenerate)
                HandleRightDegenerateCase();
            else if (Left.IsDegenerate && Right.IsDegenerate)
                ClearChildren();
        }

        /// <summary>
        /// Sets the children for the left and right side. This effectively
        /// turns it into a parent node.
        /// </summary>
        /// <param name="left">The left chlid.</param>
        /// <param name="right">The right child.</param>
        public void SetChildren(BspNode left, BspNode right)
        {
            Left = left;
            Right = right;
        }

        /// <summary>
        /// Recursively counts how many subsectors there are.
        /// </summary>
        /// <returns>The number of subsectors at and under this node.</returns>
        public int CalculateSubsectorCount()
        {
            if (Left != null && Right != null)
                return Left.CalculateSubsectorCount() + Right.CalculateSubsectorCount();
            else
                return IsSubsector ? 1 : 0;
        }

        /// <summary>
        /// Recursively counts how many parent nodes there are.
        /// </summary>
        /// <returns>The number of parents at and under this node.</returns>
        public int CalculateParentNodeCount()
        {
            if (Left != null && Right != null)
                return 1 + Left.CalculateParentNodeCount() + Right.CalculateParentNodeCount();
            else
                return 0;
        }

        /// <summary>
        /// Recursively counts how many nodes there are.
        /// </summary>
        /// <returns>The number of nodes at and under this node.</returns>
        public int CalculateTotalNodeCount()
        {
            int left = Left?.CalculateTotalNodeCount() ?? 0;
            int right = Right?.CalculateTotalNodeCount() ?? 0;
            return 1 + left + right;
        }

        private static bool EdgesAreHeadToTail(IList<SubsectorEdge> edges)
        {
            for (int i = 1; i < edges.Count; i++)
                if (edges[i - 1].End != edges[i].Start)
                    return false;

            return edges[0].Start == edges.Last().End;
        }

        private void ClearChildren()
        {
            Left = null;
            Right = null;
        }

        private void HandleLeftDegenerateCase()
        {
            if (Right != null)
                ClockwiseEdges = Right.ClockwiseEdges;
            ClearChildren();
        }

        private void HandleRightDegenerateCase()
        {
            if (Left != null)
                ClockwiseEdges = Left.ClockwiseEdges;
            ClearChildren();
        }
    }
}
