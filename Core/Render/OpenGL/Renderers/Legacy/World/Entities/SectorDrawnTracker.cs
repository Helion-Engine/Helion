using System.Collections;
using System.Linq;
using Helion.World;
using Helion.World.Geometry.Sectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

// TODO: Unused, remove?

public class SectorDrawnTracker
{
    private int m_maxSectorId = -1;
    private BitArray m_sectorWasDrawn = new BitArray(0);

    public void UpdateTo(WorldBase world)
    {
        int maxSectorId = world.Sectors.Max(sector => sector.Id);
        if (maxSectorId > m_maxSectorId)
        {
            m_maxSectorId = maxSectorId;
            m_sectorWasDrawn = new BitArray(m_maxSectorId + 1);
        }
    }

    public void Reset()
    {
        m_sectorWasDrawn.SetAll(false);
    }

    public bool HasDrawn(Sector sector)
    {
        Precondition(sector.Id <= m_maxSectorId, "Checking drawn sector which is out of range");

        return m_sectorWasDrawn.Get(sector.Id);
    }

    public void MarkDrawn(Sector sector)
    {
        Precondition(sector.Id <= m_maxSectorId, "Marking sector which is out of range");

        m_sectorWasDrawn.Set(sector.Id, true);
    }
}
