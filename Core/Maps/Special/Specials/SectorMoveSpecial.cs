using System;
using Helion.Maps.Geometry;
using Helion.Util;
using Helion.World.Physics;

namespace Helion.Maps.Special.Specials
{
    public class SectorMoveSpecial : ISpecial
    {
        public Sector Sector;

        protected SectorMoveData m_data;
        protected double m_destZ;
        protected SectorFlat m_flat;

        private PhysicsManager m_physicsManager;
        private MoveDirection m_direction;
        private int m_delayTics;
        private double m_speed;
        private double m_startZ;
        private double m_minZ;
        private double m_maxZ;
        private bool m_crushing;

        public SectorMoveSpecial(PhysicsManager physicsManager, Sector sector, double dest, SectorMoveData specialData)
        {
            Sector = sector;
            m_physicsManager = physicsManager;
            m_data = specialData;
            m_flat = m_data.SectorMoveType == SectorMoveType.Floor ? sector.Floor : sector.Ceiling;
            m_startZ = m_flat.Z;
            m_destZ = dest;

            m_direction = m_data.StartDirection;
            m_speed = m_data.StartDirection == MoveDirection.Down ? -m_data.Speed : m_data.Speed;

            m_minZ = Math.Min(m_startZ, m_destZ);
            m_maxZ = Math.Max(m_startZ, m_destZ);

            Sector.IsMoving = true;
        }

        public virtual SpecialTickStatus Tick()
        {
            if (m_delayTics > 0)
            {
                m_delayTics--;
                return SpecialTickStatus.Continue;
            }

            double destZ = MathHelper.Clamp(m_flat.Z + m_speed, m_minZ, m_maxZ);
            SectorMoveStatus status = m_physicsManager.MoveSectorZ(Sector, m_flat, m_data.SectorMoveType, m_direction, m_speed, destZ, m_data.Crush);

            if (status == SectorMoveStatus.CeilingHitFloor)
                m_destZ = m_flat.Z;

            if (status == SectorMoveStatus.Blocked)
            {
                if (m_data.MoveRepetition != MoveRepetition.None)
                    FlipMovementDirection();
            }
            else if (m_direction == m_data.StartDirection && status == SectorMoveStatus.Crush && !m_crushing)
            {
                m_crushing = true;
                if (m_data.Crush.CrushMode == ZCrushMode.DoomWithSlowDown)
                    m_speed = m_speed < 0 ? -0.1 : 0.1;
            }

            if (m_crushing && status == SectorMoveStatus.Success)
                m_crushing = false;

            if (m_flat.Z == m_destZ)
            {
                if (m_data.MoveRepetition == MoveRepetition.None || m_data.MoveRepetition == MoveRepetition.ReturnOnBlock)
                {
                    // TODO fix issue with CIString for m_data.FloorChangeTexture != null
                    if (!ReferenceEquals(m_data.FloorChangeTexture, null))
                        Sector.Floor.Texture = m_data.FloorChangeTexture;

                    Sector.IsMoving = false;
                    return SpecialTickStatus.Destroy;
                }

                FlipMovementDirection();
            }

            if (m_data.MoveRepetition == MoveRepetition.DelayReturn && m_flat.Z == m_startZ)
            {
                Sector.IsMoving = false;
                return SpecialTickStatus.Destroy;
            }

            return SpecialTickStatus.Continue;
        }

        private void FlipMovementDirection()
        {
            if (m_data.MoveRepetition == MoveRepetition.Perpetual || (m_data.MoveRepetition == MoveRepetition.DelayReturn && m_direction == m_data.StartDirection))
                m_delayTics = m_data.Delay;
                
            m_direction = m_direction == MoveDirection.Up ? MoveDirection.Down : MoveDirection.Up;
            m_destZ = m_direction == MoveDirection.Up ? m_maxZ : m_minZ;

            if (m_crushing)
            {
                m_speed = m_data.Speed;
                m_crushing = false;
            }
            else
            {
                m_speed = -m_speed;
            }
        }
    }
}
