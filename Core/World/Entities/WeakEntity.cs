using Helion.Util;
using Helion.Util.Assertion;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Helion.World.Entities;

// Represents a weak reference to an entity.
// Used for handling references that can be disposed.
// For example if a monster has a lost soul as a target it will eventually be completely disposed from the game.
// When the lost soul is disposed it will set all the weak references to null so that monster no longer has a reference to this disposed entity.
public class WeakEntity
{
    public static readonly WeakEntity Default = new();
    private static LinkedList<WeakEntity>?[] WeakEntities = new LinkedList<WeakEntity>?[128];
    private LinkedListNode<WeakEntity>? Node;

#if DEBUG
    private static int IdSet;
    public int Id;

    public WeakEntity()
    {
        Id = IdSet++;
    }

    public Entity? Entity
    {
        get => m_entity;
        set
        {
            Assert.Precondition(Id != 0, "Set default instance");
            m_entity = value;
        }
    }

    private Entity? m_entity;
#else

    public Entity? Entity;
#endif

    public static void DisposeEntity(Entity entity)
    {
        if (!GetReferences(entity, resize: false, out var references))
            return;

        var node = references.First;
        while (node != null)
        {
            node.Value.Entity = null;
            node = node.Next;
        }

        WeakEntities[entity.Id] = null;
        DataCache.Instance.FreeWeakEntityList(references);
    }

    public void Set(Entity? entity)
    {
        if (Entity == entity)
            return;

        if (entity == null)
        {
            if (ReferenceEquals(this, Default) || Entity == null)
                return;

            ClearNode();
            Entity = null;
            return;
        }

        ClearNode();
        Entity = entity;

        if (!GetReferences(entity, resize: true, out var references))
        {
            references = DataCache.Instance.GetWeakEntityList();
            WeakEntities[entity.Id] = references;
        }

        Node = references.AddLast(this);
    }

    private void ClearNode()
    {
        if (Node == null || Node.List == null)
            return;

        var list = Node.List;
        Node.List.Remove(Node);
        Node = null;

        if (list.Count == 0 && Entity != null)
        {
            WeakEntities[Entity.Id] = null;
            DataCache.Instance.FreeWeakEntityList(list);
        }
    }

    private static bool GetReferences(Entity entity, bool resize, [NotNullWhen(true)] out LinkedList<WeakEntity>? references)
    {
        if (entity.Id < WeakEntities.Length)
        {
            references = WeakEntities[entity.Id];
            return references != null;
        }

        if (resize)
        {
            LinkedList<WeakEntity>?[] newReferences = new LinkedList<WeakEntity>?[Math.Max(WeakEntities.Length * 2, entity.Id * 2)];
            Array.Copy(WeakEntities, newReferences, WeakEntities.Length);
            WeakEntities = newReferences;
            references = WeakEntities[entity.Id];
            return references != null;
        }

        references = null;
        return false;
    }

    // Unit test utility methods
    public static int ReferenceListCount() =>
        WeakEntities.Count(x => x != null);

    public static LinkedList<WeakEntity>? GetReferences(Entity entity)
    {
        if (GetReferences(entity, resize: false, out var references))
            return references;
        return null;
    }
}