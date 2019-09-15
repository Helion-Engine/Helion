using System.Collections.Generic;
using Helion.BspOld.Geometry;

namespace Helion.BspOld.States.Convex
{
    /// <summary>
    /// An instance that is responsible for convexity checking and remembering
    /// traversal ordering.
    /// </summary>
    public interface IConvexChecker
    {
        /// <summary>
        /// The state of this convex checker.
        /// </summary>
        ConvexStates States { get; }
        
        /// <summary>
        /// Loads the segments for execution.
        /// </summary>
        /// <param name="segments">The segments to check for convexity.</param>
        void Load(List<BspSegment> segments);

        /// <summary>
        /// Performs convexity checking actions.
        /// </summary>
        void Execute();
    }
}