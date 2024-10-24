using System.Collections;
using System.Linq;
using Helion.World;
using Helion.World.Geometry.Lines;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;

public class LineDrawnTracker
{
    private int m_maxLineId;
    private BitArray m_lineWasDrawn = new BitArray(0);

    public void UpdateToWorld(IWorld world)
    {
        m_maxLineId = world.Lines.Max(line => line.Id);
        m_lineWasDrawn = new BitArray(m_maxLineId + 1);
        ClearDrawnLines();
    }

    public void ClearDrawnLines()
    {
        m_lineWasDrawn.SetAll(false);
    }

    public bool HasDrawn(Line line)
    {
        Precondition(line.Id <= m_maxLineId, "Checking drawn line which is out of range");

        return m_lineWasDrawn.Get(line.Id);
    }

    public bool HasDrawn(int lineId)
    {
        Precondition(lineId <= m_maxLineId, "Checking drawn line which is out of range");

        return m_lineWasDrawn.Get(lineId);
    }

    public void MarkDrawn(Line line)
    {
        Precondition(line.Id <= m_maxLineId, "Marking line which is out of range");

        m_lineWasDrawn.Set(line.Id, true);
    }

    public void MarkDrawn(int lineId)
    {
        Precondition(lineId <= m_maxLineId, "Marking line which is out of range");

        m_lineWasDrawn.Set(lineId, true);
    }
}
