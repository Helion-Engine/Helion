using System;
using Helion.Maps.Geometry;
using Helion.World.Physics;

namespace Helion.Maps.Special.Specials
{
    public class SectorMoveSpecial : ISpecial
    {
        public Sector? Sector { get; protected set; }

        protected readonly SectorMoveData MoveData;
        protected double DestZ;
        protected SectorFlat Flat;
        protected int DelayTics;
        private PhysicsManager m_physicsManager;
        private MoveDirection m_direction;
        private double m_speed;
        private double m_startZ;
        private double m_minZ;
        private double m_maxZ;
        private bool m_crushing;

        public SectorMoveSpecial(PhysicsManager physicsManager, Sector sector, double start, double dest, SectorMoveData specialData)
        {
            Sector = sector;
            m_physicsManager = physicsManager;
            MoveData = specialData;
            Flat = MoveData.SectorMoveType == SectorMoveType.Floor ? sector.Floor : sector.Ceiling;
            m_startZ = start;
            DestZ = dest;

            m_direction = MoveData.StartDirection;
            m_speed = MoveData.StartDirection == MoveDirection.Down ? -MoveData.Speed : MoveData.Speed;

            m_minZ = Math.Min(m_startZ, DestZ);
            m_maxZ = Math.Max(m_startZ, DestZ);

            // Doom starts with the delay on perpetual movement
            if (MoveData.MoveRepetition == MoveRepetition.Perpetual)
                DelayTics = MoveData.Delay;

            Sector.ActiveMoveSpecial = this;
        }

        public virtual SpecialTickStatus Tick(long gametic)
        {
            if (DelayTics > 0)
            {
                Flat.PrevZ = Flat.Z;
                DelayTics--;
                return SpecialTickStatus.Continue;
            }

            double destZ = Flat.Z + m_speed;
            if (m_direction == MoveDirection.Down && destZ < DestZ)
                destZ = DestZ;
            else if (m_direction == MoveDirection.Up && destZ > DestZ)
                destZ = DestZ;

            SectorMoveStatus status = m_physicsManager.MoveSectorZ(Sector, Flat, MoveData.SectorMoveType, m_direction, m_speed, destZ, MoveData.Crush);

            if (status == SectorMoveStatus.Blocked)
            {
                if (MoveData.MoveRepetition != MoveRepetition.None)
                    FlipMovementDirection();
            }
            else if (status == SectorMoveStatus.Crush && IsInitCrush)
            {
                m_crushing = true;
                if (MoveData.Crush.CrushMode == ZCrushMode.DoomWithSlowDown)
                    m_speed = m_speed < 0 ? -0.1 : 0.1;
            }

            if (m_crushing && status == SectorMoveStatus.Success)
                m_crushing = false;

            if (Flat.Z == DestZ)
            {
                if (IsNonRepeat)
                {
                    // TODO fix issue with CIString for m_data.FloorChangeTexture != null
                    if (!ReferenceEquals(MoveData.FloorChangeTexture, null))
                        Sector.Floor.Texture = MoveData.FloorChangeTexture;

                    Sector.ActiveMoveSpecial = null;
                    return SpecialTickStatus.Destroy;
                }

                FlipMovementDirection();
            }

            if (IsDelayReturn && Flat.Z == m_startZ)
            {
                Sector.ActiveMoveSpecial = null;
                return SpecialTickStatus.Destroy;
            }

            return SpecialTickStatus.Continue;
        }

        public virtual void Use()
        {
        }

        protected void FlipMovementDirection()
        {
            if (MoveData.MoveRepetition == MoveRepetition.Perpetual || (IsDelayReturn && m_direction == MoveData.StartDirection))
                DelayTics = MoveData.Delay;
                
            m_direction = m_direction == MoveDirection.Up ? MoveDirection.Down : MoveDirection.Up;
            DestZ = m_direction == MoveDirection.Up ? m_maxZ : m_minZ;

            if (m_crushing)
            {
                m_speed = MoveData.Speed;
                m_crushing = false;
            }
            else
            {
                m_speed = -m_speed;
            }
        }

        private bool IsNonRepeat => MoveData.MoveRepetition == MoveRepetition.None || MoveData.MoveRepetition == MoveRepetition.ReturnOnBlock;
        private bool IsDelayReturn => MoveData.MoveRepetition == MoveRepetition.DelayReturn;
        private bool IsInitCrush => MoveData.Crush != null && m_direction == MoveData.StartDirection && !m_crushing;
    }
}
