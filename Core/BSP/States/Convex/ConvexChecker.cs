using Helion.BSP.Geometry;
using Helion.Util.Geometry;
using System.Collections.Generic;

namespace Helion.BSP.States.Convex
{
    public enum VertexCounter
    {
        LessThanThree,
        Three
    }

    public struct LinePoint
    {
        public SegmentIndex SegIndex;
        public Endpoint Endpoint;

        public LinePoint(SegmentIndex segIndex, Endpoint endpoint)
        {
            SegIndex = segIndex;
            Endpoint = endpoint;
        }
    };

    public class VertexCountTracker
    {
        public int WithOneLine = 0;
        public int WithTwoLines = 0;
    };

    public class ConvexChecker
    {
        public readonly ConvexStates State;
        private readonly Dictionary<VertexIndex, List<LinePoint>> vertexMap = new Dictionary<VertexIndex, List<LinePoint>>();
        private readonly SegmentAllocator segmentAllocator;
        private readonly VertexCountTracker vertexTracker = new VertexCountTracker();

        public ConvexChecker(SegmentAllocator allocator)
        {
            segmentAllocator = allocator;
        }

        // TODO
    }
}
