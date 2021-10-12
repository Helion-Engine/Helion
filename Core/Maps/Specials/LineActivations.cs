using System;

namespace Helion.Maps.Specials;

[Flags]
public enum LineActivations
{
    None,
    Player = 1,
    Monster = 2,
    Hitscan = 4,
    Projectile = 8,
    CrossLine = 16,
    UseLine = 32,
    ImpactLine = 64,
    LevelStart = 128,
}

