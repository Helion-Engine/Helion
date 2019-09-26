using System.Collections.Generic;
using System.Linq;
using Helion.Bsp.Geometry;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Boxes;
using NLog;

namespace Helion.Bsp.Repairer
{
    public class MapRepairer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly List<BspSegment> m_segments;
        private readonly SegmentAllocator m_segmentAllocator;
        private readonly UniformGrid<MapBlock> m_blocks;

        public MapRepairer(List<BspSegment> segments, VertexAllocator vertexAllocator, SegmentAllocator segmentAllocator)
        {
            m_segments = segments;
            m_segmentAllocator = segmentAllocator;

            Box2D bounds = vertexAllocator.Bounds();
            m_blocks = new UniformGrid<MapBlock>(bounds);
            AddSegmentsToBlocks();
        }

        public static List<BspSegment> Repair(IEnumerable<BspSegment> segments, VertexAllocator vertexAllocator, 
            SegmentAllocator segmentAllocator)
        {
            MapRepairer repairer = new MapRepairer(segments.ToList(), vertexAllocator, segmentAllocator);
            repairer.PerformRepair();
            return repairer.m_segments;
        }
        
        private static bool HasNoOverlappingTime(double startTime, double endTime)
        {
            const double epsilon = 0.0001;
            const double endEpsilon = 1.0 - epsilon;

            // Note that this may or may not do what we want for very long or
            // very short lines. With very long lines, an intersection time
            // can be small but actually have some distance. Likewise, a small
            // time for a short line might be a rounding error.
            //
            // I've seen the same vertices get mapped to +/- 0.0000000000000002
            // or some really small number, so I'm hoping that using something
            // like 0.0001 for the epsilon will magically work for all cases.
            // If we ever get any classification issues, then we'll be forced
            // to accept the segment that is in the [0, 1] range and calculate
            // the distance it goes into the segment and go from there.
            bool beforeStart = startTime <= epsilon && endTime <= epsilon;
            bool afterEnd = startTime >= endEpsilon && endTime >= endEpsilon;
            return beforeStart || afterEnd;
        }

        private void AddSegmentsToBlocks()
        {
            foreach (BspSegment segment in m_segments)
            {
                m_blocks.Iterate(segment, block =>
                { 
                    block.Segments.Add(segment);
                    if (segment.OneSided)
                        block.OneSidedSegments.Add(segment);
                });
            }
        }
        
        private void AddSegments(List<BspSegment> segsToAdd)
        {
            // Yes, this is O(n), and the only reason this exists is because
            // we call this so infrequently that it doesn't show up in the
            // profiler. I still want this fixed at some point though since
            // it'll eventually happen on a big map and we'll feel it.
            foreach (BspSegment seg in segsToAdd)
            {
                m_segments.Add(seg);
                
                m_blocks.Iterate(seg, block =>
                {
                    block.Segments.Add(seg);
                    if (seg.OneSided)
                        block.OneSidedSegments.Add(seg);
                });
            }
        }

        private void RemoveSegments(List<BspSegment> segsToRemove)
        {
            // Yes, this is O(n), and the only reason this exists is because
            // we call this so infrequently that it doesn't show up in the
            // profiler. I still want this fixed at some point though since
            // it'll eventually happen on a big map and we'll feel it.
            foreach (BspSegment seg in segsToRemove)
            {
                m_segments.Remove(seg);
                
                m_blocks.Iterate(seg, block =>
                {
                    block.Segments.Remove(seg);
                    if (seg.OneSided)
                        block.OneSidedSegments.Remove(seg);
                });
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
            while (true)
            {
                List<BspSegment> segsToRemove = new List<BspSegment>();
                List<BspSegment> segsToAdd = new List<BspSegment>();

                // TODO: We need to fully break out of both loops, but we also
                // dont want to keep revisiting them since this is a O(n^4)
                // function (the sum of `i = 1..(n choose 2) of i` has n^4 as a
                // highest order term). Breaking out wont avoid O(n^4) but all
                // maps thus far don't trigger this pathological case anyways.
                foreach (MapBlock block in m_blocks)
                {
                    foreach ((BspSegment firstSeg, BspSegment secondSeg) in block.Segments.PairCombinations())
                    {
                        if (!firstSeg.Collinear(secondSeg))
                            continue;

                        double startTime = firstSeg.ToTime(secondSeg.Start);
                        double endTime = firstSeg.ToTime(secondSeg.End);
                        if (HasNoOverlappingTime(startTime, endTime))
                            continue;

                        Log.Error("Found overlapping segments for line {0} ({1} -> {2}) and {3} ({4} -> {5})",
                            firstSeg.Line?.Id, firstSeg.Start, firstSeg.End, secondSeg.Line?.Id, secondSeg.Start,
                            secondSeg.End);

                        var overlapResolver = new CollinearOverlapResolver(firstSeg, secondSeg, startTime, endTime);
                        overlapResolver.Resolve();
                        // TODO: Do things with the resolved segment data.
                        // TODO: Add seg to be removed to the segsToRemove.
                        // TODO: Add new segs to segsToAdd.
                    }
                }

                if (segsToRemove.Empty())
                    break;
                
                // We remove them outside because we do not want to affect the
                // MapBlock's list while iterating over it.
                // TODO: Remove any segments in segsToRemove from the SegmentAllocator.
                // TODO: Add any segsToAdd to the appropriate MapBlocks.
            }
        }

        private void FixIntersectingLines()
        {
            while (true)
            {
                List<BspSegment> segsToRemove = new List<BspSegment>();
                List<BspSegment> segsToAdd = new List<BspSegment>();

                foreach (MapBlock block in m_blocks)
                {
                    foreach ((BspSegment firstSeg, BspSegment secondSeg) in block.Segments.PairCombinations())
                    {
                        // If they already meet at an endpoint, then we can
                        // exit early because if they were overlapping and
                        // have some kind of intersection elsewhere, that will
                        // have been handled by the collinear overlapper.
                        if (firstSeg.SharesAnyEndpoints(secondSeg))
                            continue;
                        
                        if (!firstSeg.IntersectionAsLine(secondSeg, out double tFirst, out double tSecond))
                            continue;
                        
                        if (!MathHelper.InNormalRange(tFirst) || !MathHelper.InNormalRange(tSecond))
                            continue;

                        HandleIntersectingSegments(firstSeg, secondSeg, tFirst, tSecond, segsToRemove, segsToAdd);
                        break;
                    }

                    // We need to abandon the loop if we're going to add stuff
                    // since we'll be mutating things and want to revisit the
                    // block. Yes, this is obviously not ideal and quickly is
                    // an O(n^4) disaster if there's tons of these, but there
                    // should never be tons of these because we rarely ever
                    // run into it.
                    if (segsToRemove.Count > 0)
                        break;
                }

                if (segsToRemove.Empty())
                    break;

                RemoveSegments(segsToRemove);
                AddSegments(segsToAdd);
            }
        }

        private void HandleIntersectingSegments(BspSegment firstSeg, BspSegment secondSeg, double tFirst, double tSecond, 
            List<BspSegment> segsToRemove, List<BspSegment> segsToAdd)
        {
            Log.Warn("Found lines that should have intersected to make a vertex but didn't, fixing geometry");

            // At this point we know they either cross, or one
            // touches the other and needs to be made into an
            // intersection. See which line touches, and if
            // neither touch then they must cross.
            if (tFirst.ApproxEquals(0) || tFirst.ApproxEquals(1))
            {
                HandleSplitAt(secondSeg, tSecond);
                Log.Info("Fixed missing intersection  vertex at {0}", secondSeg.FromTime(tSecond));
            }
            else if (tSecond.ApproxEquals(0) || tSecond.ApproxEquals(1))
            {
                HandleSplitAt(firstSeg, tFirst);
                Log.Info("Fixed missing intersection vertex at {0}", firstSeg.FromTime(tFirst));
            }
            else
            {
                HandleSplitAt(firstSeg, tFirst);
                HandleSplitAt(secondSeg, tSecond);
                Log.Info("Fixed missing intersection vertex at {0}", firstSeg.FromTime(tFirst));
            }

            void HandleSplitAt(BspSegment seg, double t)
            {
                (BspSegment segA, BspSegment segB) = m_segmentAllocator.Split(seg, t);
                segsToRemove.Add(seg);
                segsToAdd.Add(segA);
                segsToAdd.Add(segB);
            }
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