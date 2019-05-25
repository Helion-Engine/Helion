using Helion.Util.Geometry;
using System;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.BSP.Geometry
{
    using SegmentTable = Dictionary<VertexIndex, Dictionary<VertexIndex, SegmentIndex>>;

    public class SegmentAllocator
    {
        private readonly VertexAllocator vertexAllocator;
        private readonly IList<BspSegment> segments = new List<BspSegment>();
        private readonly SegmentTable segmentTable = new SegmentTable();

        public int Count => segments.Count;

        public SegmentAllocator(VertexAllocator allocator) => vertexAllocator = allocator;

        public BspSegment this[int segIndex] => segments[segIndex];
        public BspSegment this[SegmentIndex segIndex] => segments[segIndex.Index];

        private BspSegment CreateNewSegment(VertexIndex startIndex, VertexIndex endIndex, 
            SegmentIndex segIndex, int frontSectorId, int backSectorId, int lineId)
        {
            Vec2D start = vertexAllocator[startIndex];
            Vec2D end = vertexAllocator[endIndex];

            BspSegment seg = new BspSegment(start, end, startIndex, endIndex, segIndex, frontSectorId, backSectorId, lineId);
            segments.Add(seg);
            return seg;
        }

        public BspSegment GetOrCreate(VertexIndex startVertex, VertexIndex endVertex, 
            int frontSectorId = BspSegment.NoSectorId, int backSectorId = BspSegment.NoSectorId, 
            int lineId = BspSegment.MinisegLineId)
        {
            Precondition(startVertex != endVertex, "Cannot create a segment that is a point");
            Precondition(startVertex.Index >= 0 && startVertex.Index < vertexAllocator.Count, "Start vertex out of range.");
            Precondition(endVertex.Index >= 0 && endVertex.Index < vertexAllocator.Count, "End vertex out of range.");

            // TODO: Extract all this into a 'segment table' class?
            VertexIndex smallerIndex = startVertex;
            VertexIndex largerIndex = endVertex;
            if (startVertex.Index > endVertex.Index)
            {
                smallerIndex = endVertex;
                largerIndex = startVertex;
            }

            if (segmentTable.TryGetValue(smallerIndex, out var largerValues))
            {
                if (largerValues.TryGetValue(largerIndex, out SegmentIndex segIndex))
                {
                    return segments[segIndex.Index];
                }
                else
                {
                    SegmentIndex newSegIndex = new SegmentIndex(segments.Count);
                    largerValues[largerIndex] = newSegIndex;
                    return CreateNewSegment(startVertex, endVertex, newSegIndex, frontSectorId, backSectorId, lineId);
                }
            }
            else
            {
                var largerIndexDict = new Dictionary<VertexIndex, SegmentIndex>();
                segmentTable[smallerIndex] = largerIndexDict;

                SegmentIndex newSegIndex = new SegmentIndex(segments.Count);
                largerIndexDict[largerIndex] = newSegIndex;
                return CreateNewSegment(startVertex, endVertex, newSegIndex, frontSectorId, backSectorId, lineId);
            }
        }

        public bool ContainsSegment(VertexIndex startIndex, VertexIndex endIndex)
        {
            // TODO: Use the segment table class if/when created.
            VertexIndex smallerIndex = startIndex;
            VertexIndex largerIndex = endIndex;
            if (startIndex.Index > endIndex.Index)
            {
                smallerIndex = endIndex;
                largerIndex = startIndex;
            }

            if (segmentTable.TryGetValue(smallerIndex, out var largerValues))
                if (largerValues.TryGetValue(largerIndex, out SegmentIndex segIndex))
                    return true;
            return false;
        }

        public Tuple<BspSegment, BspSegment> Split(BspSegment seg, double t)
        {
            Precondition(t > 0.0 && t < 1.0, $"Trying to split BSP out of range, or at an endpoint with t = {t}");

            Vec2D middle = seg.FromTime(t);
            VertexIndex middleIndex = vertexAllocator[middle];

            BspSegment firstSeg = GetOrCreate(seg.StartIndex, middleIndex, seg.FrontSectorId, seg.BackSectorId, seg.LineId);
            BspSegment secondSeg = GetOrCreate(middleIndex, seg.EndIndex, seg.FrontSectorId, seg.BackSectorId, seg.LineId);
            return Tuple.Create(firstSeg, secondSeg);
        }

        public IList<BspSegment> ToList() => new List<BspSegment>(segments);
    }
}
