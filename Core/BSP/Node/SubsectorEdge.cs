using Helion.BSP.Geometry;
using Helion.Util.Geometry;

namespace Helion.BSP.Node
{
    public class SubsectorEdge : Seg2DBase
    {
        public const int NoSectorId = -1;

        public readonly int LineId;
        public readonly int SectorId;

        public bool IsMiniseg => LineId == BspSegment.MinisegLineId;

        public SubsectorEdge(Vec2D start, Vec2D end) : this(start, end, BspSegment.MinisegLineId, NoSectorId)
        {
        }

        public SubsectorEdge(Vec2D start, Vec2D end, int lineId, int sectorId) : base(start, end)
        {
            LineId = lineId;
            SectorId = sectorId;
        }

        // TODO
    }
}
