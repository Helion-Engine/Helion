using System;

namespace Helion.Models;

public struct EntityFlagsModel
{
    // Legacy, FlagsX is used now instead
    public int[]? Bits { get; set; }

    public int Flags1 { get; set; }
    public int Flags2 { get; set; }
    public int Flags3 { get; set; }
}
