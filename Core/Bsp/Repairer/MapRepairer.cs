using System.Collections.Generic;
using System.Linq;
using Helion.Bsp.Geometry;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Boxes;

namespace Helion.Bsp.Repairer
{
    public class MapRepairer
    {
        private readonly List<BspSegment> m_segments;
        private readonly VertexAllocator m_vertexAllocator;
        private readonly SegmentAllocator m_segmentAllocator;
        private readonly UniformGrid<MapBlock> m_anySegmentBlocks;
        private readonly UniformGrid<MapBlock> m_oneSidedSegmentBlocks;

        public MapRepairer(List<BspSegment> segments, VertexAllocator vertexAllocator, SegmentAllocator segmentAllocator)
        {
            m_segments = segments;
            m_vertexAllocator = vertexAllocator;
            m_segmentAllocator = segmentAllocator;

            Box2D bounds = vertexAllocator.Bounds();
            m_anySegmentBlocks = new UniformGrid<MapBlock>(bounds);
            m_oneSidedSegmentBlocks = new UniformGrid<MapBlock>(bounds);
            AddSegmentsToBlocks();
        }

        public static List<BspSegment> Repair(IEnumerable<BspSegment> segments, VertexAllocator vertexAllocator, 
            SegmentAllocator segmentAllocator)
        {
            MapRepairer repairer = new MapRepairer(segments.ToList(), vertexAllocator, segmentAllocator);
            repairer.PerformRepair();
            return repairer.m_segments;
        }

        private void AddSegmentsToBlocks()
        {
            foreach (BspSegment segment in m_segments)
            {
                m_anySegmentBlocks.Iterate(segment, block => block.Segments.Add(segment));
                
                if (segment.OneSided)
                    m_oneSidedSegmentBlocks.Iterate(segment, block => block.Segments.Add(segment));
            }
        }

        private void PerformRepair()
        {
            if (m_segments.Empty())
                return;
            
            FixOverlappingCollinearLines();
            FixIntersectingLines();
            FixDanglingOneSidedLines();
            FixBridgedOneSidedLines();
        }

        private void FixOverlappingCollinearLines()
        {
            List<BspSegment> segsToRemove;

            do
            {
                segsToRemove = new List<BspSegment>();
                
                foreach (MapBlock block in m_anySegmentBlocks)
                {
                    foreach ((BspSegment first, BspSegment second) in block.Segments.PairCombinations())
                    {
                        if (!first.Collinear(second))
                            continue;
                        
                        // 1) Take an arbitrary line, then find the times for all the
                        // vertices on that line, and sort them by that.
                        // TODO

                        // 2) Every vertex that has 2+ lines coming out of it will be
                        // preserved and merged.
                        // TODO

                        // 3) Endpoints are preserved always
                        // TODO

                        // 4) Anything in the middle that is not (2) will be thrown out.
                        // TODO
                    }
                }
            } 
            while (segsToRemove.Count > 0);
        }

        private void FixIntersectingLines()
        {
            // TODO
        }

        private void FixDanglingOneSidedLines()
        {
            // TODO
        }

        private void FixBridgedOneSidedLines()
        {
            // TODO
        }
    }
}