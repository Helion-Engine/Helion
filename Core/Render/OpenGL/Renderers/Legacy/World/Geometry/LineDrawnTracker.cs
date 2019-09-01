using System.Collections;
using System.Linq;
using Helion.Maps.Geometry.Lines;
using Helion.World;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry
{
    public class LineDrawnTracker
    {
        private int m_maxLineId;
        private BitArray m_lineWasDrawn = new BitArray(0);

        public void UpdateToWorld(WorldBase world)
        {
            m_maxLineId = world.Map.Lines.Max(line => line.Id);
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

        public void MarkDrawn(Line line)
        {
            Precondition(line.Id <= m_maxLineId, "Marking line which is out of range");
            
            m_lineWasDrawn.Set(line.Id, true);
        }
    }
}