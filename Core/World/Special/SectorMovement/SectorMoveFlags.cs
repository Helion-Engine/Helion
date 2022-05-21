using System;

namespace Helion.World.Special.SectorMovement
{
    [Flags]
    public enum SectorMoveFlags
    {
        None = 0,
        // Damage special is cleared when movement is complete
        ClearDamage = 1,
        // If an entity will block movement then do not calculate the difference, movement is blocked entirely.
        EntityBlockMovement = 2,
        // Allow door to clip through the floor in certain cases.
        Door = 3
    }
}
