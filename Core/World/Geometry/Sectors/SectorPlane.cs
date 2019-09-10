using Helion.Util;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Planes;

namespace Helion.World.Geometry.Sectors
{
    public class SectorPlane
    {
        public readonly int Id;
        public readonly SectorPlaneFace Facing;
        public Sector Sector { get; internal set; }
        public PlaneD? Plane;
        public double Z;
        public double PrevZ;
        public CIString Texture;
        public short LightLevel;

        public bool Sloped => Plane != null;

        public SectorPlane(int id, SectorPlaneFace facing, double z, CIString texture, short lightLevel)
        {
            Id = id;
            Facing = facing;
            Z = z;
            PrevZ = z;
            Texture = texture;
            LightLevel = lightLevel;
        }
    }
}