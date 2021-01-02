using System;

namespace Helion.Resources.Definitions.MapInfo
{
    [Flags]
    public enum MapOptions
    {
        None,
        NoIntermission = 1,
        NeedClusterText = 2
    }
}
