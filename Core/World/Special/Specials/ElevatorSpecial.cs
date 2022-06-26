using Helion.Models;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;
using Helion.World.Special.SectorMovement;

namespace Helion.World.Special.Specials;

public class ElevatorSpecial : ISectorSpecial
{
    private readonly SectorMoveSpecial m_firstMove;
    private readonly SectorMoveSpecial m_secondMove;

    public Sector Sector { get; }
    public bool IsPaused => false;

    public bool OverrideEquals => true;

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

    public ElevatorSpecial(Sector sector, SectorMoveSpecial firstMove, SectorMoveSpecial secondMove)
    {
        Sector = sector;
        m_firstMove = firstMove;
        m_secondMove = secondMove;
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

    public ISpecialModel? ToSpecialModel()
    {
        return new ElevatorSpecialModel()
        {
            FirstMove = (SectorMoveSpecialModel)m_firstMove.ToSpecialModel(),
            SecondMove = (SectorMoveSpecialModel)m_secondMove.ToSpecialModel()
        };
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ElevatorSpecial elevator)
            return false;

        return elevator.m_firstMove.Equals(m_firstMove) &&
            elevator.m_secondMove.Equals(m_secondMove);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
