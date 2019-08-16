using System.Collections;
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
            m_maxLineId = world.Map.Lines.Count;
            m_lineWasDrawn = new BitArray(m_maxLineId);
            ClearDrawnLines();
        }

        public void ClearDrawnLines()
        {
            m_lineWasDrawn.SetAll(false);
        }

        public bool HasDrawn(Line line)
        {
            Precondition(line.Id < m_maxLineId, $"Checking drawn line {line.Id} which is out of range (max ID should be {m_maxLineId})");
            
            return m_lineWasDrawn.Get(line.Id);
        }

        public void MarkDrawn(Line line)
        {
            Precondition(line.Id < m_maxLineId, $"Marking line {line.Id} which is out of range (max ID should be {m_maxLineId})");
            
            m_lineWasDrawn.Set(line.Id, true);
        }
    }
}