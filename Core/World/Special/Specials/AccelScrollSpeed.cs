using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials;

public class AccelScrollSpeed
{
    public Vec2D AccelSpeed;
    public double LastChangeZ;
    public readonly Sector Sector;
    public readonly ZDoomScroll ScrollFlags;

    private Vec2D m_speed;

    public AccelScrollSpeed(Sector changeSector, in Vec2D speed, ZDoomScroll scrollFlags)
    {
        Sector = changeSector;
        m_speed = speed;
        LastChangeZ = GetZ();
        ScrollFlags = scrollFlags;
    }

    public void Tick()
    {
        double currentZ = GetZ();
        if (LastChangeZ == currentZ)
        {
            if ((ScrollFlags & ZDoomScroll.Displacement) != 0)
                AccelSpeed = Vec2D.Zero;
            return;
        }

        double diff = currentZ - LastChangeZ;
        LastChangeZ = currentZ;
        Vec2D speed = m_speed;
        speed *= diff;

        if ((ScrollFlags & ZDoomScroll.Accelerative) != 0)
            AccelSpeed += speed;
        else
            AccelSpeed = speed;
    }

    private double GetZ() => Sector.Ceiling.Z - Sector.Floor.Z;

}
