using System;

namespace Helion.Worlds.Physics.Blockmap
{
    [Flags]
    public enum BlockmapTraverseEntityFlags
    {
        None = 0,
        Shootable = 1,
        Solid = 2,
        Corpse = 4
    }
}
