using System;

namespace Helion.Models;

public class EntityFlagsModel
{
    public static readonly EntityFlagsModel Default = new();
    public int[] Bits { get; set; } = Array.Empty<int>();
}
