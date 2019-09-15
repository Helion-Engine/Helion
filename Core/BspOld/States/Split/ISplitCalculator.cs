using System.Collections.Generic;
using Helion.BspOld.Geometry;

namespace Helion.BspOld.States.Split
{
    /// <summary>
    /// A helper class that calculates splits based on a score of a segment 
    /// relative to all the other segments.
    /// </summary>
    public interface ISplitCalculator
    {
        /// <summary>
        /// The splitter states for this calculator.
        /// </summary>
        SplitterStates States { get; }

        /// <summary>
        /// Loads the segments to evaluate which is the best splitter.
        /// </summary>
        /// <param name="segments">The segments to load.</param>
        void Load(List<BspSegment> segments);

        /// <summary>
        /// Performs some atomic step to calculate the best splitter.
        /// </summary>
        void Execute();
    }
}