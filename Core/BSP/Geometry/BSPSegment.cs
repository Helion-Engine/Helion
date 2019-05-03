using Helion.Util.Geometry;

namespace Helion.BSP.Geometry
{
    public class BspSegment : Seg2D
    {
        public const int MinisegLineId = -1;

        public readonly VertexIndex StartIndex;
        public readonly VertexIndex EndIndex;
        public readonly SegmentIndex SegIndex;
        public readonly int LineId;
        public readonly bool OneSided;

        public bool IsMiniseg => LineId == MinisegLineId;

        public BspSegment(Vec2D start, Vec2D end, VertexIndex startIndex, VertexIndex endIndex,
            SegmentIndex segIndex, int lineId, bool oneSided) : base(start, end)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            SegIndex = segIndex;
            LineId = lineId;
            OneSided = oneSided;
        }

        public Endpoint EndpointFrom(VertexIndex index) => index == StartIndex ? Endpoint.Start : Endpoint.End;
        public VertexIndex IndexFrom(Endpoint endpoint) => endpoint == Endpoint.Start ? StartIndex : EndIndex;
        public VertexIndex OppositeIndex(Endpoint endpoint) => endpoint == Endpoint.Start ? EndIndex : StartIndex;
    }
}
