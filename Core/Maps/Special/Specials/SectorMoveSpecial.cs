using System;
using Helion.Maps.Geometry;
using Helion.Util;
using Helion.World.Physics;

namespace Helion.Maps.Special.Specials
{
    public class SectorMoveSpecial : ISpecial
    {
        public Sector Sector;

        private PhysicsManager m_physicsManager;
        private LineSpecialData m_data;
        private SectorFlat m_flat;
        private MoveDirection m_direction;
        private int m_delayTics;
        private double m_speed;
        private double m_destZ;
        private double m_startZ;
        private double m_minZ;
        private double m_maxZ;

        public SectorMoveSpecial(PhysicsManager physicsManager, Sector sector, double dest, LineSpecialData specialData)
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

        public SpecialTickStatus Tick()
        {
            if (m_delayTics > 0)
            {
                m_delayTics--;
                return SpecialTickStatus.Continue;
            }

            double destZ = MathHelper.Clamp(m_flat.Z + m_speed, m_minZ, m_maxZ);
            SectorMoveStatus status = m_physicsManager.MoveSectorZ(Sector, m_flat, m_data.SectorMoveType, m_direction, m_speed, destZ);

            if (status == SectorMoveStatus.Blocked)
            {
                if (m_data.MoveRepetition != MoveRepetition.None)
                    FlipMovementDirection();
            }

            if (m_flat.Z == m_destZ)
            {
                if (m_data.MoveRepetition == MoveRepetition.None || m_data.MoveRepetition == MoveRepetition.ReturnOnBlock)
                {
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
            if (m_data.MoveRepetition == MoveRepetition.DelayReturn && m_direction == m_data.StartDirection)
                m_delayTics = 70; // TODO verify this number - in future make it a variable
            m_speed = -m_speed;
            m_direction = m_direction == MoveDirection.Up ? MoveDirection.Down : MoveDirection.Up;
            m_destZ = m_direction == MoveDirection.Up ? m_maxZ : m_minZ;
        }
    }
}
