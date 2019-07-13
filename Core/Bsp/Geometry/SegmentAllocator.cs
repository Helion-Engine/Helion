using Helion.Util.Geometry;
using System;
using System.Collections.Generic;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.Geometry
{
    // This may be converted into a class in the future. Right now this is
    // intended to be a mapping of a segments points to a segment index. The
    // first key is supposed to be the smaller of the two indices to make any
    // lookups easier (and enforce a simple ordering to prevent the reversed
    // pair from being added).
    using SegmentTable = Dictionary<VertexIndex, Dictionary<VertexIndex, SegmentIndex>>;

    /// <summary>
    /// An allocator of segments. This is required such that any splitting that
    /// occurs also properly creates new vertices through the vertex allocator.
    /// </summary>
    public class SegmentAllocator
    {
        private readonly VertexAllocator vertexAllocator;
        private readonly IList<BspSegment> segments = new List<BspSegment>();
        private readonly SegmentTable segmentTable = new SegmentTable();

        /// <summary>
        /// How many segments have been allocated.
        /// </summary>
        public int Count => segments.Count;

        public SegmentAllocator(VertexAllocator allocator) => vertexAllocator = allocator;

        /// <summary>
        /// Gets the segment for the index provided.
        /// </summary>
        /// <param name="segIndex">The index of the segment. It is an error if
        /// it is not in the range of [0, Count).</param>
        /// <returns>The segment for the index.</returns>
        public BspSegment this[int segIndex] => segments[segIndex];

        /// <summary>
        /// Looks up a segment for the index provided. This index should have
        /// been allocated by this object.
        /// </summary>
        /// <param name="segIndex">The index to look up.</param>
        /// <returns>The segment for the index.</returns>
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

        /// <summary>
        /// Gets the segment at the vertices provided if it exists, or creates
        /// it if not.
        /// </summary>
        /// <param name="startVertex">The start vertex.</param>
        /// <param name="endVertex">The end vertex.</param>
        /// <param name="frontSectorId">The front sector ID (optional).</param>
        /// <param name="backSectorId">The back sector ID (optional).</param>
        /// <param name="lineId">The line ID (optional).</param>
        /// <returns>Either a newly allocated BSP segment, or the one that
        /// already exists with the information provided.</returns>
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

        /// <summary>
        /// Checks if a segment exists with the start/end index combination.
        /// The order of the indices does not matter.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <returns>True if a segment (start, end) or (end, start) exists in
        /// this allocator, false otherwise.</returns>
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

        /// <summary>
        /// Performs a split on the segment at some time t (which should be in
        /// the normal range).
        /// </summary>
        /// <param name="seg">The segment to split.</param>
        /// <param name="t">The time to split at, which should be in (0, 1). It
        /// is an error for it to be equal to 0.0 or 1.0 as that would create
        /// a point for a segment (which is no longer a segment).</param>
        /// <returns>The two segments, where the first element is the segment
        /// from [start, middle], and the second segment element is from the
        /// [middle, end].</returns>
        public Tuple<BspSegment, BspSegment> Split(BspSegment seg, double t)
        {
            Precondition(t > 0.0 && t < 1.0, $"Trying to split BSP out of range, or at an endpoint with t = {t}");

            Vec2D middle = seg.FromTime(t);
            VertexIndex middleIndex = vertexAllocator[middle];

            BspSegment firstSeg = GetOrCreate(seg.StartIndex, middleIndex, seg.FrontSectorId, seg.BackSectorId, seg.LineId);
            BspSegment secondSeg = GetOrCreate(middleIndex, seg.EndIndex, seg.FrontSectorId, seg.BackSectorId, seg.LineId);
            return Tuple.Create(firstSeg, secondSeg);
        }

        /// <summary>
        /// Allocates a new list with all the segment references.
        /// </summary>
        /// <remarks>
        /// Intended primarily for the visual debuggers. We don't want to 
        /// expose the underlying list reference, so a whole new one is 
        /// allocated.
        /// </remarks>
        /// <returns>A list of all the existing segments.</returns>
        public IList<BspSegment> ToList() => new List<BspSegment>(segments);
    }
}
