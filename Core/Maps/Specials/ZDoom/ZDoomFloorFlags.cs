
using System;

namespace Helion.Maps.Specials.ZDoom
{
    [Flags]
    public enum ZDoomFloorFlags
    {
        None = 0,
        CopyFloorRemoveSpecial = 1,
        CopyFloor = 2,
        CopyFloorAndSpecial = 3,
        TriggerNumericModel = 4,
        RaiseFloor = 8,
        Crush = 16
    }
}
