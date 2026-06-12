using System;

namespace Helion.World.Physics;

[Flags]
public enum SectorMoveStatus
{
    None = 0,
    Success = 1,
    Blocked = 2,
    Crushed = 4,
    Stop = 8
}

public static class SectorMoveStatusExtensions
{
    public static SectorMoveStatus Merge(this SectorMoveStatus status, SectorMoveStatus other)
    {
        status |= other;
        if ((status & SectorMoveStatus.Blocked) != 0)
            status &= ~SectorMoveStatus.Success;
        return status;
    }
}