using Helion.Geometry.Vectors;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials
{
    public class AccelScrollSpeed
    {
        public Vec2D AccelSpeed;
        public double LastChangeZ;
        public readonly Sector Sector;

        private Vec2D m_speed;

        public AccelScrollSpeed(Sector changeSector, in Vec2D speed)
        {
            Sector = changeSector;
            m_speed = speed;
            LastChangeZ = Sector.Floor.Z;
        }

        public void Tick()
        {
            if (LastChangeZ == Sector.Floor.Z)
                return;

            double diff = Sector.Floor.Z - LastChangeZ;
            LastChangeZ = Sector.Floor.Z;
            Vec2D speed = m_speed;
            speed *= diff;

            AccelSpeed += speed;
        }
    }
}
