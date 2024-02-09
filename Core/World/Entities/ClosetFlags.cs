using System;

namespace Helion.World.Entities;

[Flags]
public enum ClosetFlags
{
    None,
    MonsterCloset = 1,
    ClosetLook = 2,
    ClosetChase = 4
}
