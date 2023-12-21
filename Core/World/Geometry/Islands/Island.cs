using Helion.World.Bsp;
using System.Collections.Generic;

namespace Helion.World.Geometry.Islands;

/// <summary>
/// A collection of lines and sectors that are reachable from each other by
/// traversing adjacent subsectors.
/// </summary>
public class Island
{
    public readonly int Id;
    public readonly List<BspSubsector> Subsectors = new();
    public readonly List<int> LineIds = new();
    public bool IsMonsterCloset;
    public bool IsVooDooCloset;
    public int InitialMonsterCount;

    public Island(int id)
    {
        Id = id;
    }
}
