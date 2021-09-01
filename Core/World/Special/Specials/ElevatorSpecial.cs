using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;
using Helion.World.Special.SectorMovement;

namespace Helion.World.Special.Specials
{
    public class ElevatorSpecial : ISectorSpecial
    {
        private readonly SectorMoveSpecial m_firstMove;
        private readonly SectorMoveSpecial m_secondMove;

        public Sector Sector { get; }
        public bool IsPaused => false;

        public ElevatorSpecial(IWorld world, Sector sector, double floorDestZ, double speed,
            MoveDirection moveDirection, SectorSoundData soundData)
        {
            Sector = sector;

            var floor = new SectorMoveSpecial(world, sector, Sector.Floor.Z, floorDestZ,
                new SectorMoveData(SectorPlaneFace.Floor, moveDirection, MoveRepetition.None, speed, 0), soundData);
            var ceiling = new SectorMoveSpecial(world, sector, Sector.Ceiling.Z, floorDestZ + sector.Ceiling.Z - sector.Floor.Z,
                new SectorMoveData(SectorPlaneFace.Ceiling, moveDirection, MoveRepetition.None, speed, 0), soundData);

            // Sector plane that can potentially be blocked needs to moved first
            if (moveDirection == MoveDirection.Up)
            {
                m_firstMove = floor;
                m_secondMove = ceiling;
            }
            else
            {
                m_firstMove = ceiling;
                m_secondMove = floor;
            }
        }

        public SpecialTickStatus Tick()
        {
            m_firstMove.Tick();
            if (m_firstMove.MoveStatus == SectorMoveStatus.Blocked)
                m_secondMove.ResetInterpolation();
            else
                return m_secondMove.Tick();

            return SpecialTickStatus.Continue;
        }

        public void ResetInterpolation()
        {
            m_firstMove.ResetInterpolation();
            m_secondMove.ResetInterpolation();
        }

        public void FinalizeDestroy()
        {
            m_firstMove.FinalizeDestroy();
            m_secondMove.FinalizeDestroy();
        }

        public void Pause()
        {
            // Not required
        }

        public void Resume()
        {
            // Not required
        }

        public bool Use(Entity entity)
        {
            return false;
        }
    }
}
