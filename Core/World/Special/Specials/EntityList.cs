using Helion.World.Entities;
using System.Collections.Generic;

namespace Helion.World.Special.Specials;

internal ref struct EntityList
{
    private readonly LinkedList<Entity>? Entities;
    private LinkedListNode<Entity>? CurrentNode;
    private Entity? Entity;

    public EntityList(LinkedList<Entity> entities)
    {
        Entities = entities;
        CurrentNode = entities.First;
    }

    public EntityList(Entity entity)
    {
        Entity = entity;
    }

    public readonly Entity? Current()
    {
        if (Entities == null)
            return Entity;

        return CurrentNode?.Value;
    }

    public Entity? Advance()
    {
        if (Entities == null)
        {
            Entity = null;
            return null;
        }

        if (CurrentNode == null)
            return null;

        CurrentNode = CurrentNode.Next;
        return CurrentNode?.Value;
    }
}
