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
    private static List<WeakEntity>?[] WeakEntities = new List<WeakEntity>?[128];

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
        if (!GetReferences(entity, resize: false, out List<WeakEntity>? references))
            return;

        for (int i = 0; i < references.Count; i++)
            references[i].Entity = null;

        WeakEntities[entity.Id] = null;
        DataCache.Instance.FreeWeakEntityList(references);
    }

    public void Set(Entity? entity)
    {
        Entity = entity;

        if (entity == null)
            return;

        if (!GetReferences(entity, resize: true, out List<WeakEntity>? references))
        {
            references = DataCache.Instance.GetWeakEntityList();
            WeakEntities[entity.Id] = references;
        }

        references.Add(this);
    }

    private static bool GetReferences(Entity entity, bool resize, [NotNullWhen(true)] out List<WeakEntity>? references)
    {
        if (entity.Id < WeakEntities.Length)
        {
            references = WeakEntities[entity.Id];
            return references != null;
        }

        if (resize)
        {
            List<WeakEntity>?[] newReferences = new List<WeakEntity>?[Math.Max(WeakEntities.Length * 2, entity.Id * 2)];
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

    public static List<WeakEntity>? GetReferences(Entity entity)
    {
        if (GetReferences(entity, resize: false, out var references))
            return references;
        return null;
    }
}