using Helion.Util;
using Helion.Util.Geometry;
using System;
using System.Collections.Generic;

namespace Helion.BSP.Geometry
{
    using SegmentTable = Dictionary<VertexIndex, Dictionary<VertexIndex, SegmentIndex>>;

    public struct SegmentIndex
    {
        public readonly int Index;

        public SegmentIndex(int index) => Index = index;

        public static bool operator ==(SegmentIndex first, SegmentIndex second) => first.Index == second.Index;
        public static bool operator !=(SegmentIndex first, SegmentIndex second) => first.Index != second.Index;

        public override bool Equals(object obj) => obj is SegmentIndex index && Index == index.Index;
        public override int GetHashCode() => HashCode.Combine(Index);
    }

    public class SegmentAllocator
    {
        private readonly VertexAllocator vertexAllocator;
        private readonly List<BspSegment> segments = new List<BspSegment>();
        private readonly SegmentTable segmentTable = new SegmentTable();

        public SegmentAllocator(VertexAllocator allocator) => vertexAllocator = allocator;

        public BspSegment this[SegmentIndex segIndex] => segments[segIndex.Index];

        private BspSegment CreateNewSegment(VertexIndex startIndex, VertexIndex endIndex, 
            SegmentIndex segIndex, int lineId, bool oneSided)
        {
            Vec2D start = vertexAllocator[startIndex];
            Vec2D end = vertexAllocator[endIndex];

            BspSegment seg = new BspSegment(start, end, startIndex, endIndex, segIndex, lineId, oneSided);
            segments.Add(seg);
            return seg;
        }

        public BspSegment GetOrCreate(VertexIndex startVertex, VertexIndex endVertex, int lineId, bool oneSided)
        {
            Assert.Precondition(startVertex != endVertex, "Cannot create a segment that is a point");
            Assert.Precondition(startVertex.Index >= 0 && startVertex.Index < vertexAllocator.Count, "Start vertex out of range.");
            Assert.Precondition(endVertex.Index >= 0 && endVertex.Index < vertexAllocator.Count, "End vertex out of range.");

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
                    return CreateNewSegment(startVertex, endVertex, newSegIndex, lineId, oneSided);
                }
            }
            else
            {
                var largerIndexDict = new Dictionary<VertexIndex, SegmentIndex>();
                segmentTable[smallerIndex] = largerIndexDict;

                SegmentIndex newSegIndex = new SegmentIndex(segments.Count);
                largerIndexDict[largerIndex] = newSegIndex;
                return CreateNewSegment(startVertex, endVertex, newSegIndex, lineId, oneSided);
            }
        }

        public Tuple<BspSegment, BspSegment> Split(BspSegment seg, double t)
        {
            Assert.Precondition(t > 0.0 && t < 1.0, $"Trying to split BSP out of range, or at an endpoint with t = {t}");

            Vec2D middle = seg.FromTime(t);
            VertexIndex middleIndex = vertexAllocator[middle];

            BspSegment firstSeg = GetOrCreate(seg.StartIndex, middleIndex, seg.LineId, seg.OneSided);
            BspSegment secondSeg = GetOrCreate(middleIndex, seg.EndIndex, seg.LineId, seg.OneSided);
            return Tuple.Create(firstSeg, secondSeg);
        }
    }
}
