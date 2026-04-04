using Helion.World.Entities;
using System;
using System.Collections.Generic;

namespace Helion.World.Special.Specials;

internal ref struct EntityList
{
    private readonly LinkedList<Entity>? Entities;
    private LinkedListNode<Entity>? CurrentNode;
    private Entity? Entity;
    private string? ClassName;



    public EntityList(LinkedList<Entity> entities)
    {
        Entities = entities;
        CurrentNode = entities.First;
    }

    public EntityList(LinkedList<Entity> entities, string className)
    {
        Entities = entities;
        CurrentNode = entities.First;
        ClassName = className;
        SetToValidClassNode(ClassName);
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

        if (ClassName != null)
            SetToValidClassNode(ClassName);

        return CurrentNode?.Value;
    }

    private void SetToValidClassNode(string className)
    {
        while (CurrentNode != null && !CurrentNode.Value.Definition.IsType(className))
            CurrentNode = CurrentNode.Next;
    }
}
