using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;
using System.Collections.Generic;

namespace Helion.World.Geometry;

/// <summary>
/// A collection of lines and sectors that are reachable from
/// </summary>
public class Island
{
    public readonly int Id;
    public readonly List<Sector> Sectors = new();
    public readonly List<Line> Lines = new();
    public readonly List<BspSubsector> Subsectors = new();

    public Island(int id)
    {
        Id = id;
    }
}
