using Helion.BSP.Geometry;
using System.Collections.Generic;

namespace Helion.BSP.States.Partition
{
    public class Partitioner
    {
        public PartitionStates States = new PartitionStates();
        private readonly BspConfig config;
        private readonly VertexAllocator vertexAllocator;
        private readonly SegmentAllocator segmentAllocator;

        public Partitioner(BspConfig config, VertexAllocator vertexAllocator, SegmentAllocator segmentAllocator)
        {
            this.config = config;
            this.vertexAllocator = vertexAllocator;
            this.segmentAllocator = segmentAllocator;
        }

        public void Load(BspSegment splitter, List<BspSegment> segments)
        {
            States = new PartitionStates();
            States.Splitter = splitter;
            States.SegsToSplit = segments;
        }

        public virtual void Execute()
        {
            // TODO
        }
    }
}
