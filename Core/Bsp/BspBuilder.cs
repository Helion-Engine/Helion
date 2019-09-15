using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Bsp.Node;
using Helion.Maps;

namespace Helion.Bsp
{
    public abstract class BspBuilder
    {
        protected readonly BspConfig BspConfig;
        protected readonly VertexAllocator VertexAllocator;
        protected readonly CollinearTracker CollinearTracker;
        protected readonly SegmentAllocator SegmentAllocator;

        protected BspBuilder(BspConfig config, IMap map)
        {
            BspConfig = config;
            VertexAllocator = new VertexAllocator(config.VertexWeldingEpsilon);
            CollinearTracker = new CollinearTracker(config.VertexWeldingEpsilon);
            SegmentAllocator = new SegmentAllocator(VertexAllocator, CollinearTracker);

            List<BspSegment> segments = ReadMapLines(map);
            // TODO: JunctionClassifier.Add(segments);
        }

        public abstract BspNode? Build();

        private List<BspSegment> ReadMapLines(IMap map)
        {
            List<BspSegment> segments = new List<BspSegment>();
            foreach (IBspUsableLine line in map.GetLines())
            {
                int startIndex = VertexAllocator[line.StartPosition];
                int endIndex = VertexAllocator[line.EndPosition];
                BspSegment segment = SegmentAllocator.GetOrCreate(startIndex, endIndex, line);
                segments.Add(segment);
            }
            
            return SegmentChainPruner.Prune(segments);
        }
    }
}