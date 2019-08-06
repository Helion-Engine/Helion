using System;
using Helion.Maps.Geometry;
using Helion.Util;

namespace Helion.Maps.Special.Specials
{
    public class SectorMoveSpecial : ISpecial
    {
        public Sector Sector;

        // TODO actually verify this value
        private const int SetEntityToFloorSpeedMax = 5;

        private LineSpecialData m_data;
        private SectorFlat m_flat;
        private MoveDirection m_direction;
        private int m_delayTics;
        private double m_speed;
        private double m_destZ;
        private double m_startZ;
        private double m_minZ;
        private double m_maxZ;

        public SectorMoveSpecial(Sector sector, double dest, LineSpecialData specialData)
        {
            Sector = sector;
            m_data = specialData;
            m_flat = m_data.SectorMoveType == SectorMoveType.Floor ? sector.Floor : sector.Ceiling;
            m_startZ = m_flat.Z;
            m_destZ = dest;

            m_direction = m_data.StartDirection;
            m_speed = m_data.StartDirection == MoveDirection.Down ? m_data.Speed * -1 : m_data.Speed;

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
            bool blocked = EntityBlocksMovement(destZ);

            if (blocked)
            {
                if (m_data.MoveRepitition != MoveRepitition.None)
                    FlipMovementDirection();
            }
            else
            {
                // At slower speeds we need to set entities to the floor
                // Otherwise the player will fall and hit the floor repeatedly creating a weird bouncing effect
                // TODO maybe this belongs in physics manager
                if (m_data.SectorMoveType == SectorMoveType.Floor && m_direction == MoveDirection.Down && m_data.Speed < SetEntityToFloorSpeedMax)
                {
                    foreach (var entity in Sector.Entities)
                    {
                        if (entity.OnGround && !entity.IsFlying && entity.HighestFloorSector.Id == Sector.Id)
                            entity.SetZ(destZ);
                    }
                }

                m_flat.Plane.MoveZ(destZ - m_flat.Z);
                m_flat.Z = destZ;

                // TODO temporary - just for my own sanity until renderer displays floor changes
                System.Console.WriteLine(m_flat.Z);
            }

            if (m_flat.Z == m_destZ)
            {
                if (m_data.MoveRepitition == MoveRepitition.None)
                {
                    Sector.IsMoving = false;
                    return SpecialTickStatus.Destroy;
                }

                FlipMovementDirection();
            }

            if (m_data.MoveRepitition == MoveRepitition.RepeatOnce && m_flat.Z == m_startZ)
            {
                Sector.IsMoving = false;
                return SpecialTickStatus.Destroy;
            }

            return SpecialTickStatus.Continue;
        }

        private void FlipMovementDirection()
        {
            if (m_direction == m_data.StartDirection)
                m_delayTics = 70;
            m_speed *= -1;
            m_direction = m_direction == MoveDirection.Up ? MoveDirection.Down : MoveDirection.Up;
            m_destZ = m_direction == MoveDirection.Up ? m_maxZ : m_minZ;
        }

        private bool EntityBlocksMovement(double destZ)
        {
            // Save the Z value because we are only checking if the dest is valid
            // If the move is invalid because of a blocking entity then it will not be set to destZ
            // TODO maybe this belongs in physics manager
            double saveZ = m_flat.Z;
            double thingZ;
            m_flat.Z = destZ;

            foreach (var entity in Sector.Entities)
            {
                if (entity.OnGround)
                    thingZ = Sector.Floor.Z;
                else
                    thingZ = entity.Position.Z;

                if (thingZ + entity.Height > Sector.Ceiling.Z)
                {
                    m_flat.Z = saveZ;
                    return true;
                }
            }

            m_flat.Z = saveZ;
            return false;
        }
    }
}
