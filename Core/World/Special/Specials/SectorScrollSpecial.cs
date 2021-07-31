using Helion.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials
{
    public enum SectorScrollType
    {
        Scroll,
        Carry
    }

    class SectorScrollSpecial : ISpecial
    {
        public readonly SectorPlane SectorPlane;

        private readonly SectorScrollType m_type;
        private readonly Sector? m_changeScrollSector;
        private Vec2D m_speed;
        private Vec2D m_speedChange;
        private double m_lastChangeZ;

        public SectorScrollSpecial(SectorScrollType type, SectorPlane sectorPlane, in Vec2D speed, Sector? changeScroll = null)
        {
            m_type = type;
            SectorPlane = sectorPlane;
            m_speed = speed;
            m_changeScrollSector = changeScroll;
            if (m_changeScrollSector != null)
                m_lastChangeZ = m_changeScrollSector.Floor.Z;

            SectorPlane.SectorScrollData = new();
        }

        public SpecialTickStatus Tick()
        {
            Vec2D speed = m_speed;

            if (m_changeScrollSector != null)
            {
                double diff = m_changeScrollSector.Floor.Z - m_lastChangeZ;
                m_lastChangeZ = m_changeScrollSector.Floor.Z;
                speed *= diff;

                m_speedChange += speed;
                speed = m_speedChange;
            }

            if (speed == Vec2D.Zero)
                return SpecialTickStatus.Continue;

            if (m_type == SectorScrollType.Scroll)
            {
                SectorPlane.SectorScrollData!.LastOffset = SectorPlane.SectorScrollData!.Offset;
                SectorPlane.SectorScrollData!.Offset += speed;
                SectorPlane.Sector.DataChanges |= SectorDataTypes.Offset;
            }
            else if (m_type == SectorScrollType.Carry && SectorPlane == SectorPlane.Sector.Floor)
            {
                foreach (var entity in SectorPlane.Sector.Entities)
                {
                    if (entity.Flags.NoBlockmap || entity.Flags.NoClip || !entity.OnGround || entity.HighestFloorSector != SectorPlane.Sector)
                        continue;

                    entity.Velocity.X += speed.X;
                    entity.Velocity.Y += speed.Y;
                }
            }

            return SpecialTickStatus.Continue;
        }

        public void ResetInterpolation()
        {
            if (m_type == SectorScrollType.Scroll)
                SectorPlane.SectorScrollData!.LastOffset = SectorPlane.SectorScrollData!.Offset;
        }

        public bool Use(Entity entity)
        {
            return false;
        }
    }
}
