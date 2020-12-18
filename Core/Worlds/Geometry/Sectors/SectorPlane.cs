using Helion.Util.Geometry.Planes;
using Helion.Worlds.Textures;

namespace Helion.Worlds.Geometry.Sectors
{
    public class SectorPlane
    {
        public readonly int Id;
        public readonly SectorPlaneFace Facing;
        public readonly PlaneD Plane;
        public Sector Sector { get; internal set; }
        public double Z;
        public double PrevZ;
        public IWorldTexture Texture;
        public short LightLevel;

        public SectorPlane(int id, SectorPlaneFace facing, double z, IWorldTexture texture, short lightLevel)
        {
            Id = id;
            Facing = facing;
            Z = z;
            PrevZ = z;
            Texture = texture;
            LightLevel = lightLevel;
            Plane = new PlaneD(0, 0, 1.0, -z);

            // We are okay with things blowing up violently if someone forgets
            // to assign it, because that is such a critical error on the part
            // of the developer if this ever happens that it's deserved. Fixing
            // this would lead to some very messy logic, and when this is added
            // to a parent object, it will add itself for us. If this can be
            // fixed in the future with non-messy code, go for it.
            Sector = null !;
        }
    }
}