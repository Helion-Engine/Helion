using Helion.Util.Geometry;

namespace Helion.BSP.Geometry
{
    public class BspSegment : Seg2D
    {
        public const int NoSectorId = -1;
        public const int MinisegLineId = -1;

        public readonly VertexIndex StartIndex;
        public readonly VertexIndex EndIndex;
        public readonly SegmentIndex SegIndex;
        public readonly int LineId;
        public readonly int FrontSectorId;
        public readonly int BackSectorId;
        public readonly bool OneSided;

        public bool IsMiniseg => LineId == MinisegLineId;
        public bool TwoSided => !OneSided;

        public BspSegment(Vec2D start, Vec2D end, VertexIndex startIndex, VertexIndex endIndex,
            SegmentIndex segIndex, int frontSectorId, int backSectorId = NoSectorId, int lineId = MinisegLineId) : 
            base(start, end)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            SegIndex = segIndex;
            FrontSectorId = frontSectorId;
            BackSectorId = backSectorId;
            LineId = lineId;
            OneSided = (backSectorId == NoSectorId);
        }

        public BspSegment(BspSegment seg) : base(seg.Start, seg.End)
        {
            StartIndex = seg.StartIndex;
            EndIndex = seg.EndIndex;
            SegIndex = seg.SegIndex;
            FrontSectorId = seg.FrontSectorId;
            BackSectorId = seg.BackSectorId;
            LineId = seg.LineId;
            OneSided = seg.OneSided;
        }

        public Endpoint EndpointFrom(VertexIndex index) => index == StartIndex ? Endpoint.Start : Endpoint.End;
        public Endpoint OppositeEndpoint(VertexIndex index) => index == StartIndex ? Endpoint.End : Endpoint.Start;
        public VertexIndex IndexFrom(Endpoint endpoint) => endpoint == Endpoint.Start ? StartIndex : EndIndex;
        public VertexIndex OppositeIndex(Endpoint endpoint) => endpoint == Endpoint.Start ? EndIndex : StartIndex;

        public override string ToString()
        {
            return $"({Start}) -> ({End}) [front={FrontSectorId}, back={BackSectorId}, lineId={LineId}, oneSided={OneSided}]";
        }
    }
}
