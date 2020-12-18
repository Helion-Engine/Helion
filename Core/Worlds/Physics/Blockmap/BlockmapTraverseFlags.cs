using System;

namespace Helion.Worlds.Physics.Blockmap
{
    [Flags]
    public enum BlockmapTraverseFlags
    {
        Entities = 1,
        Lines = 2,
        StopOnOneSidedLine = 4
    }
}
