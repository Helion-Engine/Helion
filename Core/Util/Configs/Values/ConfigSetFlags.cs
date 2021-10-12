using System;

namespace Helion.Util.Configs.Values;

/// <summary>
/// Flags that indicate when setting should occur. If a field is set with
/// this flag, then whenever such a flag is propagated as a request to
/// update values, such changes will be applied.
/// </summary>
[Flags]
public enum ConfigSetFlags
{
    Normal = 0x0,
    OnNewWorld = 0x1
}

