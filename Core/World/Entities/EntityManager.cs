using System.Collections.Generic;
using Helion.Maps;
using Helion.Maps.Geometry;
using Helion.Maps.Things;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Decorate;
using Helion.Util;
using Helion.Util.Container.Linkable;
using Helion.Util.Geometry;
using Helion.World.Blockmaps;
using Helion.World.Bsp;
using Helion.World.Entities.Players;
using Helion.World.Physics;
using NLog;
using static Helion.Util.Assertion.Assert;
using Console = System.Console;

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
        private readonly Map m_map;
        private readonly PhysicsManager m_physicsManager;
        private readonly WorldBase m_world;

        public EntityManager(WorldBase world, ArchiveCollection archiveCollection, BspTree bspTree,
            Blockmap blockmap, PhysicsManager physicsManager, Map map)
        {
            m_archiveCollection = archiveCollection;
            m_blockmap = blockmap;
            m_bspTree = bspTree;
            m_map = map;
            m_physicsManager = physicsManager;
            m_world = world;
        }

        public Entity Create(ActorDefinition definition, Vec3D position, double angle)
        {
            Sector sector = m_bspTree.ToSector(position);
            Entity entity = new Entity(definition, position, angle, sector);
            
            LinkableNode<Entity> node = Entities.Add(entity);
            entity.EntityListNode = node;
            
            LinkToWorld(entity);
            
            return entity;
        }
        
        public Player CreatePlayer(int playerNumber, ActorDefinition definition, Vec3D position, double angle)
        {
            Precondition(!Players.ContainsKey(playerNumber), $"Trying to create player {playerNumber} twice");
            
            Entity playerEntity = Create(definition, position, angle);
            Player player = new Player(playerNumber, playerEntity);
            Players[playerNumber] = player;
            return player;
        }
        
        public void LinkToWorld(Entity entity)
        {
            m_blockmap.Link(entity);
            m_physicsManager.Link(entity);
        }
        
        public Player? CreatePlayer(int playerStartLocation)
        {
            if (Players.TryGetValue(playerStartLocation, out Player existingPlayer))
            {
                Fail("Trying to create player twice (temporary code!)");
                return existingPlayer;
            }
                
            Invariant(m_archiveCollection.Definitions.Decorate.Contains(Constants.PlayerClass), "Missing player class, cannot spawn player");
            ActorDefinition playerDefinition = m_archiveCollection.Definitions.Decorate[Constants.PlayerClass];

            // TODO: Use playerStartLocation w/ cached player spawn locations.
            // TODO: This is only for player 1, which is obviously not what we want.
            foreach (Thing thing in m_map.Things)
            {
                if (thing.EditorNumber != 1)
                    continue;

                Console.WriteLine($"Spawn 1 angle: {thing.AngleRadians} (is {thing.AngleRadians / MathHelper.TwoPi * 360} degrees)");
                return CreatePlayer(1, playerDefinition, thing.Position, thing.AngleRadians);
            }
            
            Log.Warn($"No player 1 spawns in map {m_map.Name}");
            return null;
        }
    }
}