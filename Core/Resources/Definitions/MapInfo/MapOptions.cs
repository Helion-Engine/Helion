using System;

namespace Helion.Resources.Definitions.MapInfo
{
    [Flags]
    public enum MapOptions
    {
        None,
        NoIntermission = 1,
        NeedClusterText = 2,
        AllowMonsterTelefrags = 4,
        NoCrouch = 8,
        NoJump = 16
    }
}
