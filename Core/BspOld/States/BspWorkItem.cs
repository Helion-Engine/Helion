using Helion.BspOld.Geometry;
using System.Collections.Generic;
using static Helion.Util.Assertion.Assert;

namespace Helion.BspOld.States
{
    /// <summary>
    /// A collection of work item information for some stage in the BSP tree.
    /// </summary>
    /// <remarks>
    /// This holds information for the full cycle of events. By events, it is
    /// the convex check, splitter finder, partitioning, miniseg generation
    /// cycle. When all the steps are done, two children work items are to be
    /// created from the left/right split.
    /// </remarks>
    public class BspWorkItem
    {
        /// <summary>
        /// The path for a root node.
        /// </summary>
        public const string RootWorkPath = "";

        /// <summary>
        /// A list of all the segments for this work item.
        /// </summary>
        public IList<BspSegment> Segments;

        /// <summary>
        /// The left/right path that was taken to get to this node. If it's the
        /// root node, it is equal to <see cref="RootWorkPath"/>.
        /// </summary>
        public string BranchPath;

        public BspWorkItem(IList<BspSegment> segments, string branchPath = RootWorkPath)
        {
            Precondition(segments.Count > 0, "Should never have zero segments for a work item");

            Segments = segments;
            BranchPath = branchPath;
        }
    }
}
