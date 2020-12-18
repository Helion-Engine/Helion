using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assertion.Assert;

namespace Helion.Worlds.Entities.Spawn
{
    /// <summary>
    /// A collection of all the spawn entity locations in the map.
    /// </summary>
    public class SpawnLocations
    {
        private readonly IDictionary<int, IList<Worlds.Entities.Entity>> m_playerStarts = new Dictionary<int, IList<Worlds.Entities.Entity>>();
        private readonly IList<Worlds.Entities.Entity> m_deathmatchStarts = new List<Worlds.Entities.Entity>();
        private readonly IList<Worlds.Entities.Entity> m_cooperativeStarts = new List<Worlds.Entities.Entity>();

        /// <summary>
        /// Reads the entity and determines if it is a spawn location or not.
        /// If it is, it will track it and utilize it for spawning from.
        /// </summary>
        /// <param name="entity">The entity to evaluate.</param>
        public void AddPossibleSpawnLocation(Worlds.Entities.Entity entity)
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
        /// <param name="playerIndex">The index of the player, zero based.
        /// </param>
        /// <returns>The spawn location that was last added, or null if it is
        /// unable to be found (implying no spawn locations present).</returns>
        public Worlds.Entities.Entity? GetPlayerSpawn(int playerIndex)
        {
            return m_playerStarts.TryGetValue(playerIndex, out IList<Worlds.Entities.Entity>? spawns) ? spawns.LastOrDefault() : null;
        }

        private void AddPlayerSpawn(Worlds.Entities.Entity entity, int playerIndex)
        {
            Precondition(playerIndex >= 0, "Cannot add a negative player index");

            if (m_playerStarts.TryGetValue(playerIndex, out IList<Worlds.Entities.Entity>? spawns))
            {
                Precondition(!spawns.Contains(entity), "Trying to add the same entity twice to the deathmatch spawns");
                spawns.Add(entity);
            }
            else
                m_playerStarts[playerIndex] = new List<Worlds.Entities.Entity> { entity };
        }

        private void AddDeathmatchStart(Worlds.Entities.Entity entity)
        {
            Precondition(!m_deathmatchStarts.Contains(entity), "Trying to add the same entity twice to the deathmatch spawns");

            m_deathmatchStarts.Add(entity);
        }
    }
}