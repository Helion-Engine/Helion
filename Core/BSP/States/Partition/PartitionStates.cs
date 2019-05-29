using Helion.BSP.Geometry;
using System.Collections.Generic;

namespace Helion.BSP.States.Partition
{
    /// <summary>
    /// All the states the partitioner can be in.
    /// </summary>
    public enum PartitionState
    {
        Loaded,
        Working,
        Finished
    }

    /// <summary>
    /// Stateful information for the partitioner.
    /// </summary>
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
