using System.Collections.Generic;
using Helion.Maps;
using Helion.Maps.Geometry;
using Helion.Maps.Things;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Container.Linkable;
using Helion.Util.Geometry;
using Helion.World.Blockmaps;
using Helion.World.Bsp;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Players;
using Helion.World.Physics;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities
{
    public class EntityManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly LinkableList<Entity> Entities = new LinkableList<Entity>();
        public readonly Dictionary<int, Player> Players = new Dictionary<int, Player>();
        private readonly ArchiveCollection m_archiveCollection;
        private readonly Blockmap m_blockmap;
        private readonly BspTree m_bspTree;
        private readonly IMap m_map;
        private readonly PhysicsManager m_physicsManager;
        private readonly WorldBase m_world;
        private readonly AvailableIndexTracker m_entityIdTracker = new AvailableIndexTracker();
        private readonly EntityDefinitionComposer m_definitionComposer;

        public EntityManager(WorldBase world, ArchiveCollection archiveCollection, BspTree bspTree,
            Blockmap blockmap, PhysicsManager physicsManager, IMap map)
        {
            m_archiveCollection = archiveCollection;
            m_blockmap = blockmap;
            m_bspTree = bspTree;
            m_map = map;
            m_physicsManager = physicsManager;
            m_world = world;
            m_definitionComposer = new EntityDefinitionComposer(archiveCollection);

            PopulateFrom(map);
        }

        public Entity Create(EntityDefinition definition, Vec3D position, double angle)
        {
            int id = m_entityIdTracker.Next();
            Sector sector = m_bspTree.ToSector(position);
            Entity entity = new Entity(id, definition, position, angle, sector);
            
            LinkableNode<Entity> node = Entities.Add(entity);
            entity.EntityListNode = node;
            
            m_physicsManager.LinkToWorld(entity);

            entity.ResetInterpolation();
            
            return entity;
        }
        
        public Player CreatePlayer(int playerNumber, EntityDefinition definition, Vec3D position, double angle)
        {
            Precondition(!Players.ContainsKey(playerNumber), $"Trying to create player {playerNumber} twice");
            
            Entity playerEntity = Create(definition, position, angle);
            Player player = new Player(playerNumber, playerEntity);
            Players[playerNumber] = player;
            return player;
        }
        
        // TODO: Change this method name, it clashes with another...
        public Player? CreatePlayer(int playerStartLocation)
        {
            if (Players.TryGetValue(playerStartLocation, out Player? existingPlayer))
            {
                Fail("Trying to create player twice (temporary code!)");
                return existingPlayer;
            }
                
            EntityDefinition? playerDefinition = m_definitionComposer[Constants.PlayerClass];
            if (playerDefinition == null)
            {
                Log.Error("Missing player definition class {0}, cannot create player", Constants.PlayerClass);
                return null;
            }

            // TODO: Use playerStartLocation w/ cached player spawn locations.
            // TODO: This is only for player 1, which is obviously not what we want.
            foreach (Thing thing in m_map.Things)
            {
                if (thing.EditorNumber != 1)
                    continue;

                return CreatePlayer(1, playerDefinition, thing.Position, thing.AngleRadians);
            }
            
            Log.Warn($"No player 1 spawns in map {m_map.Name}");
            return null;
        }
        
        private void PopulateFrom(IMap map)
        {
            foreach (Thing thing in map.Things)
            {
                EntityDefinition? definition = m_definitionComposer[thing.EditorNumber];
                if (definition == null)
                {
                    Log.Warn("Cannot find entity by editor number at {1}", thing.EditorNumber, thing.Position.To2D());
                    continue;
                }

                Entity entity = Create(definition, thing.Position, thing.AngleRadians);
                Log.Info("Made entity {0}", entity.Definition.Name);
            }
        }
    }
}