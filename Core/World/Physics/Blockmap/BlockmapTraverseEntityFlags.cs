using System;

namespace Helion.World.Physics.Blockmap;

[Flags]
public enum BlockmapTraverseEntityFlags
{
    None = 0,
    Shootable = 1,
    Solid = 2,
    Corpse = 4,
    NotCorpse = 8
}

