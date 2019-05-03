using Helion.Util.Geometry;

namespace Helion.BSP.Geometry
{
    public struct SectorLine
    {
        public const int NoLineToSectorId = -1;

        public readonly Vec2D Delta;
        public readonly int FrontSectorId;
        public readonly int BackSectorId;

        public bool OneSided => BackSectorId == NoLineToSectorId;
        public bool TwoSided => !OneSided;

        public SectorLine(Vec2D delta, int frontSectorId, int backSectorId)
        {
            Delta = delta;
            FrontSectorId = frontSectorId;
            BackSectorId = backSectorId;
        }
    }
}
