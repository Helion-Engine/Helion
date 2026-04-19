using Helion.Models;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;
using Helion.World.Special.SectorMovement;
using System.Collections.Generic;

namespace Helion.World.Special.Specials;

public class ElevatorSpecial : SectorMoveSpecial
{
    private readonly SectorMoveSpecial m_firstMove;
    private readonly SectorMoveSpecial m_secondMove;

    public override bool IsPaused => false;

    public override bool OverrideEquals => true;

    public override bool MultiSector => true;
    public override void GetSectors(List<(Sector, SectorPlane)> data)
    {
        data.Add((m_firstMove.Sector, m_firstMove.SectorPlane));
        data.Add((m_secondMove.Sector, m_secondMove.SectorPlane));
    }

    public ElevatorSpecial(IWorld world, Sector sector, double floorDestZ, double speed,
        MoveDirection moveDirection, SectorSoundData soundData)
    {
        Sector = sector;

        var floor = world.DataCache.GetSectorMoveSpecial(world, sector, Sector.Floor.Z, floorDestZ,
            new SectorMoveData(SectorPlaneFace.Floor, moveDirection, MoveRepetition.None, speed, 0), soundData);
        var ceiling = world.DataCache.GetSectorMoveSpecial(world, sector, Sector.Ceiling.Z, floorDestZ + sector.Ceiling.Z - sector.Floor.Z,
            new SectorMoveData(SectorPlaneFace.Ceiling, moveDirection, MoveRepetition.None, speed, 0), soundData);

        // Sector plane that can potentially be blocked needs to moved first
        // Reverse when sector controls 3D sectors
        if (sector.TaggedSectors3D.Length > 0)
            moveDirection = moveDirection == MoveDirection.Up ? MoveDirection.Down : MoveDirection.Up;

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

    public override SpecialTickStatus Tick()
    {
        m_firstMove.Tick();
        if (m_firstMove.MoveStatus == SectorMoveStatus.Blocked)
            m_secondMove.ResetInterpolation();
        else
            return m_secondMove.Tick();

        return SpecialTickStatus.Continue;
    }

    public override void ResetInterpolation()
    {
        m_firstMove.ResetInterpolation();
        m_secondMove.ResetInterpolation();
    }

    public override void FinalizeDestroy()
    {
        m_firstMove.FinalizeDestroy();
        m_secondMove.FinalizeDestroy();
    }

    public override void Free()
    {

    }

    public override void Pause()
    {
        // Not required
    }

    public override void Resume()
    {
        // Not required
    }

    public override bool Use(Entity entity)
    {
        return false;
    }

    public ElevatorSpecialModel ToSpecialElevatorModel()
    {
        return new()
        {
            FirstMove = m_firstMove.ToSpecialModel(),
            SecondMove = m_secondMove.ToSpecialModel()
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
