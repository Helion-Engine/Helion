using Helion.BSP.Geometry;
using System.Collections.Generic;

namespace Helion.BSP.States.Partition
{
    public enum PartitionState
    {
        Loaded,
        Working,
        Finished
    }

    public class PartitionStates
    {
        public PartitionState State = PartitionState.Loaded;
        public BspSegment? Splitter;
        public int CurrentSegToPartitionIndex = 0;
        public IList<BspSegment> SegsToSplit = new List<BspSegment>();
        public IList<BspSegment> LeftSegments = new List<BspSegment>();
        public IList<BspSegment> RightSegments = new List<BspSegment>();
        public HashSet<VertexIndex> CollinearVertices = new HashSet<VertexIndex>();
    };
}
