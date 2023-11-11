using System.Collections.Generic;
using System.Linq;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;

namespace Helion.Render.Common.World.SelfReferencing;

// A connected group of subsectors on both sides of a closed-loop self-referencing
// set of lines.
public class SelfRefSubsectorIsland
{
    public readonly int Id;
    private readonly List<int> m_subsectorIds;
    private readonly List<Line> m_borders;
    private readonly Sector m_enclosingSector;

    public SelfRefSubsectorIsland(int id, HashSet<int> subsectorIds, HashSet<Line> borders)
    {
        Id = id;
        m_subsectorIds = subsectorIds.ToList();
        m_borders = borders.ToList();
        m_enclosingSector = FindEnclosingSector();
    }

    private Sector FindEnclosingSector()
    {
        // TODO
        return m_borders[0].Front.Sector;
    }
}