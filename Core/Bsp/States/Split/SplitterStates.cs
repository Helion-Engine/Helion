using System.Collections.Generic;
using Helion.Bsp.Geometry;

namespace Helion.Bsp.States.Split
{
    /// <summary>
    /// The stateful information for the split calculator.
    /// </summary>
    public class SplitterStates
    {
        /// <summary>
        /// The current state of the splitting.
        /// </summary>
        public SplitterState State = SplitterState.Loaded;
        
        /// <summary>
        /// All the segments to be considered for splitting.
        /// </summary>
        public List<BspSegment> Segments = new List<BspSegment>();
        
        /// <summary>
        /// The best splitter we've found so far, or null if we haven't looked
        /// at any segments yet.
        /// </summary>
        public BspSegment? BestSplitter;
        
        /// <summary>
        /// The index into <see cref="Segments"/> that we will be looking at
        /// next. This will be out of range at the very end of the iteration.
        /// </summary>
        public int CurrentSegmentIndex = 0;
        
        /// <summary>
        /// The lowest segment splitter score seen so far.
        /// </summary>
        public int BestSegScore = int.MaxValue;
        
        /// <summary>
        /// The current segment score.
        /// </summary>
        public int CurrentSegScore = int.MaxValue;
    }
}