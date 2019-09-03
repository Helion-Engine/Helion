using Helion.Util.Geometry;

namespace Helion.World.Geometry.Sectors
{
    public class SectorPlane
    {
        public readonly int Id;
        public readonly Sector Sector;
        public readonly SectorPlaneFace Facing;
        public readonly PlaneD? Plane;
        public double Z;
    }
}