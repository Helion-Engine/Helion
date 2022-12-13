using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
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
    public readonly List<Line> Lines = new();
    public bool IsMonsterCloset;
    public int InitialMonsterCount;

    public Island(int id)
    {
        Id = id;
    }
}
