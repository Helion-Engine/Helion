using Helion.Util.Container;
using Helion.World.Entities.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities.Spawn;

/// <summary>
/// A collection of all the spawn entity locations in the map.
/// </summary>
public class SpawnLocations
{
    private IWorld m_world;

    private readonly Dictionary<int, IList<WeakEntity>> m_playerStarts = new();
    private readonly IList<Entity> m_deathmatchStarts = new List<Entity>();
    private readonly IList<Entity> m_cooperativeStarts = new List<Entity>();

    public SpawnLocations(IWorld world)
    {
        m_world = world;
    }

    /// <summary>
    /// Reads the entity and determines if it is a spawn location or not.
    /// If it is, it will track it and utilize it for spawning from.
    /// </summary>
    /// <param name="entity">The entity to evaluate.</param>
    public void AddPossibleSpawnLocation(Entity entity)
    {
        int editorId = entity.Definition.EditorId ?? int.MinValue;

        switch (editorId)
        {
            case 1:
            case 2:
            case 3:
            case 4:
                AddPlayerSpawn(entity, editorId - 1);
                break;
            case 4001:
            case 4002:
            case 4003:
            case 4004:
                AddPlayerSpawn(entity, editorId - 3997);
                break;
            case 11:
                AddDeathmatchStart(entity);
                break;
        }
    }

    /// <summary>
    /// Gets the spawn for the provided player index. The index starts at
    /// zero, so the first player is 0, second player is 1, etc.
    /// </summary>
    /// <param name="playerIndex">The index of the player, zero based.</param>
    /// <param name="mapInit">If the map is being initialized. Doom had different checks based on init.
    /// The init check would only check against other spawning players.</param>
    /// <returns>The spawn location that was last added, or null if it is
    /// unable to be found (implying no spawn locations present).</returns>
    public Entity? GetPlayerSpawn(int playerIndex, bool mapInit)
    {
        if (m_playerStarts.TryGetValue(playerIndex, out IList<WeakEntity>? spawns))
        {
            Entity? spawn = GetLastPlayerSpawn(spawns);
            if (spawn != null)
            {
                if (mapInit && !PlayerBlock(spawn))
                    return spawn;

                if (!mapInit && !m_world.IsPositionBlocked(spawn))
                    return spawn;
            }
        }

        foreach (var item in m_playerStarts)
        {
            Entity? spawn = GetLastPlayerSpawn(item.Value);
            if (spawn == null || m_world.IsPositionBlocked(spawn))
                continue;

            return spawn;
        }

        return null;
    }

    private static Entity? GetLastPlayerSpawn(IList<WeakEntity> spawns)
    {
        for (int i = spawns.Count - 1; i >= 0; i--)
        {
            if (spawns[i].Entity != null)
                return spawns[i].Entity;
        }

        return null;
    }

    public IList<Entity> GetPlayerSpawns(int playerIndex)
    {
        if (m_playerStarts.TryGetValue(playerIndex, out IList<WeakEntity>? spawns))
            return spawns.Where(x => x.Entity != null).Select(x => x.Entity!).ToList();

        return Array.Empty<Entity>();
    }

    private static bool PlayerBlock(Entity spawn)
    {
        DynamicArray<Entity> entities = spawn.World.DataCache.GetEntityList();
        spawn.World.BlockmapTraverser.GetSolidEntityIntersections(spawn, entities);
        bool blocked = false;
        for (int i= 0; i < entities.Length; i++)
        {
            if (entities[i].IsPlayer)
            {
                blocked = true;
                break;
            }
        }
        spawn.World.DataCache.FreeEntityList(entities);
        return blocked;
    }

    private void AddPlayerSpawn(Entity entity, int playerIndex)
    {
        Precondition(playerIndex >= 0, "Cannot add a negative player index");

        if (m_playerStarts.TryGetValue(playerIndex, out IList<WeakEntity>? spawns))
        {
            Precondition(!spawns.Any(x => entity.Id.Equals(x.Entity?.Id)), "Trying to add the same entity twice to the deathmatch spawns");
            spawns.Add(WeakEntity.GetReference(entity));
        }
        else
            m_playerStarts[playerIndex] = new List<WeakEntity> { WeakEntity.GetReference(entity) };
    }

    private void AddDeathmatchStart(Entity entity)
    {
        Precondition(!m_deathmatchStarts.Contains(entity), "Trying to add the same entity twice to the deathmatch spawns");

        m_deathmatchStarts.Add(entity);
    }
}
