using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry;
using Helion.Maps;
using Helion.Maps.Components;
using Helion.Maps.Shared;
using Helion.Resources.Archives.Collection;
using Helion.Models;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Players;
using Helion.World.Entities.Spawn;
using Helion.World.Geometry.Sectors;
using Helion.World.Sound;
using MoreLinq.Extensions;
using NLog;
using Helion.World.Entities.Definition.Flags;

namespace Helion.World.Entities
{
    public class EntityManager : IDisposable
    {
        public class WorldModelPopulateResult
        {
            public WorldModelPopulateResult(IList<Player> players, Dictionary<int, Entity> entities)
            {
                Players = players;
                Entities = entities;
            }

            public readonly IList<Player> Players;
            public readonly Dictionary<int, Entity> Entities;
        }


        public const int NoTid = 0;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly LinkableList<Entity> Entities = new LinkableList<Entity>();
        public readonly SpawnLocations SpawnLocations;
        public readonly WorldBase World;

        private readonly WorldSoundManager m_soundManager;
        private readonly Dictionary<int, ISet<Entity>> TidToEntity = new Dictionary<int, ISet<Entity>>();      

        public readonly EntityDefinitionComposer DefinitionComposer;
        public readonly List<Player> Players = new List<Player>();
        public readonly List<Player> VoodooDolls = new List<Player>();

        private int m_id;

        public EntityManager(WorldBase world, ArchiveCollection archiveCollection, WorldSoundManager soundManager)
        {
            World = world;
            m_soundManager = soundManager;
            SpawnLocations = new SpawnLocations(world);
            DefinitionComposer = new EntityDefinitionComposer(archiveCollection);
        }

        public static bool ZHeightSet(double z)
        {
            return z != Fixed.Lowest().ToDouble() && z != 0.0;
        }

        public IEnumerable<Entity> FindByTid(int tid)
        {
            return TidToEntity.TryGetValue(tid, out ISet<Entity>? entities) ? entities : Enumerable.Empty<Entity>();
        }

        public Entity? Create(string className, in Vec3D pos)
        {
            var def = DefinitionComposer.GetByName(className);
            if (def != null)
                return Create(def, pos, 0.0, 0.0, 0);
            return null;
        }

        public Entity Create(EntityDefinition definition, Vec3D position, double zHeight, double angle, int tid, bool init = false)
        {
            int id = m_id++;
            Sector sector = World.BspTree.ToSector(position);
            position.Z = GetPositionZ(sector, in position, zHeight);
            Entity entity = new Entity(id, tid, definition, position, angle, sector, this, m_soundManager, World);

            if (entity.Definition.Properties.FastSpeed > 0 && World.SkillDefinition.IsFastMonsters(entity.World.Config))
                entity.Properties.Speed = entity.Definition.Properties.FastSpeed;              

            // This only needs to happen on map population
            if (init && !ZHeightSet(zHeight))
            {
                entity.SetZ(entity.Sector.ToFloorZ(position), false);
                entity.PrevPosition = entity.Position;
            }

            FinishCreatingEntity(entity, zHeight);
            return entity;
        }

        public void Destroy(Entity entity)
        {
            // TODO: Remove from spawns if it is a spawn.
            
            // To avoid more object allocation and deallocation, I'm going to
            // leave empty sets in the map in case they get populated again.
            // Most maps wouldn't even approach a number that high for us to
            // worry about. If it ever becomes an issue, then we can add a line
            // of code that removes empty sets here as well.
            if (TidToEntity.TryGetValue(entity.ThingId, out ISet<Entity>? entities))
                entities.Remove(entity);

            entity.Dispose();
        }

        public Player CreatePlayer(int playerIndex, Entity spawnSpot, bool isVoodooDoll)
        {
            Player player;
            EntityDefinition? playerDefinition = DefinitionComposer.GetByName(Constants.PlayerClass);
            if (playerDefinition == null)
            {
                Log.Error("Missing player definition class {0}, cannot create player {1}", Constants.PlayerClass, playerIndex);
                throw new HelionException("Missing the default player class, should never happen");
            }

            player = CreatePlayerEntity(playerIndex, playerDefinition, spawnSpot.Position, 0.0, spawnSpot.AngleRadians);
            player.IsVooDooDoll = isVoodooDoll;

            if (isVoodooDoll)
                VoodooDolls.Add(player);
            else
                Players.Add(player);

            return player;
        }

        public void PopulateFrom(IMap map)
        {
            List<Entity> relinkEntities = new List<Entity>();

            foreach (IThing mapThing in map.GetThings())
            {
                if (!ShouldSpawn(mapThing))
                    continue;

                EntityDefinition? definition = DefinitionComposer.GetByID(mapThing.EditorNumber);
                if (definition == null)
                {
                    Log.Warn("Cannot find entity by editor number {0} at {1}", mapThing.EditorNumber, mapThing.Position.To2D());
                    continue;
                }

                if (World.Config.Game.NoMonsters && definition.Flags.IsMonster)
                    continue;

                double angleRadians = MathHelper.ToRadians(mapThing.Angle);
                Vec3D position = mapThing.Position.ToDouble();
                // position.Z is the potential zHeight variable, not the actual z position. We need to pass it to Create to ensure the zHeight is set
                Entity entity = Create(definition, position, position.Z, angleRadians, mapThing.ThingId, true);
                if (mapThing.Flags.Ambush)
                    entity.Flags.Ambush = mapThing.Flags.Ambush;

                if (!entity.Flags.ActLikeBridge && ZHeightSet(position.Z))
                    relinkEntities.Add(entity);
                PostProcessEntity(entity);
            }

            //Relink entities with a z-height only, this way they can properly stack with other things in the map now that everything exists
            for (int i = 0; i < relinkEntities.Count; i++)
            {
                relinkEntities[i].UnlinkFromWorld();
                World.Link(relinkEntities[i]);
                relinkEntities[i].PrevPosition = relinkEntities[i].Position;
            }
        }

        public WorldModelPopulateResult PopulateFrom(WorldModel worldModel)
        {
            List<Player> players = new List<Player>();
            Dictionary<int, Entity> entities = new Dictionary<int, Entity>();
            for (int i = 0; i < worldModel.Entities.Count; i++)
            {
                var entityModel = worldModel.Entities[i];
                var definition = DefinitionComposer.GetByName(entityModel.Name);
                if (definition != null)
                {
                    var entity = new Entity(entityModel, definition, this, m_soundManager, World);
                    var node = Entities.Add(entity);
                    entity.EntityListNode = node;

                    entities.Add(entity.Id, entity);
                }
            }

            for (int i = 0; i < worldModel.Players.Count; i++)
            {
                bool isVoodooDoll = players.Any(x => x.PlayerNumber == worldModel.Players[i].Number);
                Player? player = CreatePlayerFromModel(worldModel.Players[i], entities, isVoodooDoll);
                if (player == null)
                    Log.Error($"Failed to create player {worldModel.Players[i].Name}.");
                else
                    players.Add(player);
            }

            m_id = entities.Keys.Max() + 1;

            for (int i = 0; i < worldModel.Entities.Count; i++)
            {
                var entityModel = worldModel.Entities[i];
                var entity = entities[entityModel.Id];

                if (entityModel.Owner.HasValue)
                    entities.TryGetValue(entityModel.Owner.Value, out entity.Owner);
                if (entityModel.Target.HasValue)
                    entities.TryGetValue(entityModel.Target.Value, out entity.Target);
                if (entityModel.Tracer.HasValue)
                    entities.TryGetValue(entityModel.Tracer.Value, out entity.Tracer);
            }

            return new WorldModelPopulateResult(players, entities);
        }

        private Player? CreatePlayerFromModel(PlayerModel playerModel, Dictionary<int, Entity> entities, bool isVoodooDoll)
        {
            var playerDefinition = DefinitionComposer.GetByName(playerModel.Name);
            if (playerDefinition != null)
            {
                Player player = new Player(playerModel, entities, playerDefinition, this, m_soundManager, World);
                player.IsVooDooDoll = isVoodooDoll;

                var node = Entities.Add(player);
                player.EntityListNode = node;
                entities.Add(player.Id, player);

                if (isVoodooDoll)
                    VoodooDolls.Add(player);
                else
                    Players.Add(player);

                return player;
            }

            return null;
        }

        private bool ShouldSpawn(IThing mapThing)
        {
            // Ignore difficulty on spawns...
            if ((mapThing.EditorNumber > 0 && mapThing.EditorNumber < 5) || mapThing.EditorNumber == 1)
                return true;

            // TODO: These should be offloaded into SinglePlayerWorld...
            if (mapThing.Flags.MultiPlayer)
                return false;

            switch ((SkillLevel)World.SkillDefinition.SpawnFilter)
            {
                case SkillLevel.VeryEasy:
                case SkillLevel.Easy:
                    return mapThing.Flags.Easy;
                case SkillLevel.Medium:
                    return mapThing.Flags.Medium;
                case SkillLevel.Hard:
                case SkillLevel.Nightmare:
                    return mapThing.Flags.Hard;
                default:
                    return false;
            }
        }

        private static double GetPositionZ(Sector sector, in Vec3D position, double zHeight)
        {
            if (ZHeightSet(zHeight))
                return zHeight + sector.ToFloorZ(position);

            return position.Z;
        }

        private void FinishCreatingEntity(Entity entity, double zHeight)
        {          
            LinkableNode<Entity> node = Entities.Add(entity);
            entity.EntityListNode = node;

            World.Link(entity);

            if (entity.Flags.SpawnCeiling)
            {
                double offset = ZHeightSet(zHeight) ? -zHeight : 0;
                entity.SetZ(entity.Sector.ToCeilingZ(entity.Position) - entity.Height + offset, false);
            }

            entity.ResetInterpolation();
            entity.SetSpawnState();
            entity.SpawnPoint = entity.Position;
        }

        private void PostProcessEntity(Entity entity)
        {
            SpawnLocations.AddPossibleSpawnLocation(entity);

            if (entity.ThingId != NoTid)
            {
                if (TidToEntity.TryGetValue(entity.ThingId, out ISet<Entity>? entities))
                    entities.Add(entity);
                else
                    TidToEntity.Add(entity.ThingId, new HashSet<Entity> { entity });
            }
        }
        
        private Player CreatePlayerEntity(int playerNumber, EntityDefinition definition, Vec3D position, double zHeight, double angle)
        {
            int id = m_id++;
            Sector sector = World.BspTree.ToSector(position);
            position.Z = GetPositionZ(sector, position, zHeight);
            Player player = new Player(id, 0, definition, position, angle, sector, this, m_soundManager, World, playerNumber);

            var armor = DefinitionComposer.GetByName(Inventory.ArmorClassName);
            if (armor != null)
                player.Inventory.Add(armor, 0);
            
            FinishCreatingEntity(player, zHeight);
            
            return player;
        }

        public void Dispose()
        {
            Entities.ForEach(entity => entity.Dispose());
        }
    }
}