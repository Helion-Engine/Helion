using System;

namespace Helion.World.Static;

[Flags]
public enum SectorDynamic
{
    None = 0,
    Movement = 1,
    TransferHeights = 2,
    Scroll = 4,
    ScrollY = 8,
}
