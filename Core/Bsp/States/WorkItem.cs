using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Bsp.Node;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.States
{
    /// <summary>
    /// A collection of work item information for some stage in the BSP tree.
    /// </summary>
    public class WorkItem
    {
        /// <summary>
        /// The node to be used for this work item.
        /// </summary>
        public readonly BspNode Node;
        
        /// <summary>
        /// A list of all the segments for this work item.
        /// </summary>
        public readonly List<BspSegment> Segments;
        
        /// <summary>
        /// The left/right path that was taken to get to this node. If it's the
        /// root node, it will be an empty string.
        /// </summary>
        public readonly string BranchPath;
        
        /// <summary>
        /// Creates a new work item that can be operated on in a BSP pass.
        /// </summary>
        /// <param name="node">The node to be operated on.</param>
        /// <param name="segments">The list of segments for this work item.
        /// </param>
        /// <param name="branchPath">The path taken to get here. This should be
        /// upper case.</param>
        public WorkItem(BspNode node, List<BspSegment> segments, string branchPath = "")
        {
            Precondition(segments.Count > 0, "Should never have zero segments for a work item");
            Precondition(branchPath == branchPath.ToUpper(), "Should be using upper case BSP branch paths");
            
            Node = node;
            Segments = segments;
            BranchPath = branchPath;
        }
    }
}