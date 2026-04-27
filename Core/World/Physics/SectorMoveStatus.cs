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