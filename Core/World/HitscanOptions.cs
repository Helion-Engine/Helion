using System;

namespace Helion.World;

[Flags]
public enum HitScanOptions
{
    Default = 0,
    PassThroughEntities = 1,
    DrawRail = 2,
}