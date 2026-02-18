using Helion.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics.Blockmap;

namespace Helion.World;

internal struct HitValues3D
{
    public BlockmapIntersect? MinReturnValue3D;
    public Vec3D MinIntersect3D;
    public Sector? MinHitSector3D;
    public SectorPlane? MinHitSectorPlane3D;
    public Entity? ValidateEntity3D;
    public Vec3D ValidateEntityIntersect3D;
    public double ValidateEntityDistance3D;
    public double MinDistanceSquared3D;

    public HitValues3D()
    {
        MinReturnValue3D = null;
        MinIntersect3D = default;
        MinHitSector3D = null;
        MinHitSectorPlane3D = null;
        ValidateEntity3D = null;
        ValidateEntityIntersect3D = default;
        ValidateEntityDistance3D = double.MaxValue;
        MinDistanceSquared3D = double.MaxValue;
    }

    public void ClearHit()
    {
        MinReturnValue3D = null;
        MinHitSector3D = null;
        MinHitSectorPlane3D = null;
    }
}
