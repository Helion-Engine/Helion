using System;

namespace Helion.Maps.Specials.ZDoom;

[Flags]
public enum ZDoomScroll
{
    None = 0,
    Displacement = 1,
    Accelerative = 2,
    Line = 4,
    // This was added for MBF21 line scrollers. No idea what ZDoom has done here - may not line up.
    OffsetSpeed = 8,
}
