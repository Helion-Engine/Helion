using Helion;
using Helion.World;
using Helion.World.Bsp;
using Helion.World.Geometry;
using Helion.World.Geometry.Islands;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;
using System.Collections.Generic;

namespace Helion.World.Geometry.Islands;

/// <summary>
/// A collection of lines and sectors that are reachable from each other by
/// traversing adjacent subsectors.
/// </summary>
public class Island
{
    public readonly int Id;
    public readonly List<Sector> Sectors = new();
    public readonly List<Line> Lines = new();

    public Island(int id)
    {
        Id = id;
    }
}
