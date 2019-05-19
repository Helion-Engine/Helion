using Helion.BSP.Geometry;
using System.Collections.Generic;

namespace Helion.BSP.States.Split
{
    public enum SplitterState
    {
        Loaded,
        Working,
        Finished
    }

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
