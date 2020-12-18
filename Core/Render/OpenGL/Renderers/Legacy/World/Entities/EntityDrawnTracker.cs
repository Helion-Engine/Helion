using System.Collections;
using System.Linq;
using Helion.World;
using Helion.World.Entities;
using static Helion.Util.Assertion.Assert;
using Entity = Helion.Worlds.Entities.Entity;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities
{
    public class EntityDrawnTracker
    {
        private int m_maxEntityId;
        private BitArray m_entityWasDrawn = new BitArray(0);

        public void Reset(Worlds.World world)
        {
            int maxEntityId = world.Entities.Max(entity => entity.Id) + 1;
            if (maxEntityId > m_maxEntityId)
            {
                m_maxEntityId = maxEntityId;
                m_entityWasDrawn = new BitArray(m_maxEntityId);
            }
            
            m_entityWasDrawn.SetAll(false);
        }

        public bool HasDrawn(Entity entity)
        {
            Precondition(entity.Id <= m_maxEntityId, "Checking drawn entity which is out of range");
            
            return m_entityWasDrawn.Get(entity.Id);
        }

        public void MarkDrawn(Entity entity)
        {
            Precondition(entity.Id <= m_maxEntityId, "Marking entity which is out of range");
            
            m_entityWasDrawn.Set(entity.Id, true);
        }
    }
}