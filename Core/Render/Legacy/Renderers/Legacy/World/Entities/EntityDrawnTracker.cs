using System.Collections;
using System.Linq;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World;
using Helion.World.Entities;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Legacy.Renderers.Legacy.World.Entities;

public class EntityDrawnTracker
{
    private int m_maxEntityId;
    private BitArray m_entityWasDrawn = new BitArray(0);

    public void Reset(WorldBase world)
    {
        int maxEntityId = GetMax(world) + 1;
        if (maxEntityId > m_maxEntityId)
        {
            m_maxEntityId = maxEntityId;
            m_entityWasDrawn = new BitArray(m_maxEntityId);
        }

        m_entityWasDrawn.SetAll(false);
    }

    private static int GetMax(WorldBase world)
    {
        int max = -1;
        LinkableNode<Entity>? node = world.Entities.Head;
        while (node != null)
        {
            if (node.Value.Id > max)
                max = node.Value.Id;
            node = node.Next;
        }
        return max;
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
