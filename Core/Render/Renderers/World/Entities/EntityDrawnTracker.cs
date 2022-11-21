using System.Collections;
using Helion;
using Helion.Render;
using Helion.Render.Renderers.World.Entities;
using Helion.World;
using Helion.World.Entities;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Renderers.World.Entities;

public class EntityDrawnTracker
{
    private int m_maxEntityId;
    private BitArray m_entityWasDrawn = new(0);

    public void Reset(IWorld world)
    {
        int maxEntityId = world.EntityManager.MaxId + 1;
        if (maxEntityId > m_maxEntityId)
        {
            m_maxEntityId = maxEntityId * 2;
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
