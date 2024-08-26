using System;

namespace Helion.World.Static;

[Flags]
public enum SectorDynamic
{
    None = 0,
    Movement = 1,
    TransferHeights = 2,
    TransferHeightStatic = 4,
    Scroll = 8,
    ScrollY = 16,
    Alpha = 32
}
