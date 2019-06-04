using Helion.Bsp.Geometry;
using System.Collections.Generic;

namespace Helion.Bsp.States.Split
{
    /// <summary>
    /// All the states for a split calculator object.
    /// </summary>
    public enum SplitterState
    {
        Loaded,
        Working,
        Finished
    }

    /// <summary>
    /// The stateful information for the split calculator.
    /// </summary>
    public class SplitterStates
    {
        public SplitterState State = SplitterState.Loaded;
        public IList<BspSegment> Segments = new List<BspSegment>();
        public BspSegment? BestSplitter;
        public int CurrentSegmentIndex = 0;
        public int BestSegScore = int.MaxValue;
        public int CurrentSegScore = int.MaxValue;
    };
}
