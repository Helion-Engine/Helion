using System;

namespace Helion.Maps.Specials.ZDoom;

[Flags]
public enum ZDoomGenericFlags
{
    None = 0,
    CopyTxRemoveSpecial = 1,
    CopyTx = 2,
    CopyTxAndSpecial = 3,
    TriggerNumericModel = 4,
    Raise = 8,
    Crush = 16
}

