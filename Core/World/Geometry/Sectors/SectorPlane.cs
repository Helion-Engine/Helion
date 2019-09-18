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
        public int TextureHandle;
        public short LightLevel;

        public bool Sloped => Plane != null;

        public SectorPlane(int id, SectorPlaneFace facing, double z, CIString texture, int textureHandle, short lightLevel)
        {
            Id = id;
            Facing = facing;
            Z = z;
            PrevZ = z;
            Texture = texture;
            TextureHandle = textureHandle;
            LightLevel = lightLevel;

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