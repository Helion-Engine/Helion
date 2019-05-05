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
        public BspSegment Splitter = null;
        public int CurrentSegToPartitionIndex = 0;
        public List<BspSegment> SegsToSplit = new List<BspSegment>();
        public List<BspSegment> LeftSegments = new List<BspSegment>();
        public List<BspSegment> RightSegments = new List<BspSegment>();
        public HashSet<VertexIndex> CollinearVertices = new HashSet<VertexIndex>();
    };
}
