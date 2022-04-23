using Helion.Util;
using System.Collections.Generic;

namespace Helion.World.Entities;

// Represents a weak reference to an entity.
// Used for handling references that can be disposed.
// For example if a monster has a lost soul as a target it will eventually be completely disposed from the game.
// When the lost soul is disposed it will set all the weak references to null so that monster no longer has a reference to this disposed entity.
public class WeakEntity
{
    public static readonly WeakEntity Default = new();
    public readonly static Dictionary<int, List<WeakEntity>> WeakEntities = new();

    public Entity? Entity;

    public static void DisposeEntity(Entity entity)
    {
        if (!WeakEntities.TryGetValue(entity.Id, out List<WeakEntity>? references))
            return;

        for (int i = 0; i < references.Count; i++)
            references[i].Entity = null;

        WeakEntities.Remove(entity.Id);
        DataCache.Instance.FreeWeakEntityList(references);
    }

    public void Set(Entity? entity)
    {
        Entity = entity;

        if (entity == null)
            return;

        if (!WeakEntities.TryGetValue(entity.Id, out List<WeakEntity>? references))
        {
            references = DataCache.Instance.GetWeakEntityList();
            WeakEntities[entity.Id] = references;
        }

        references.Add(this);
    }
}