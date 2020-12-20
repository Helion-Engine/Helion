using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Maps;
using Helion.Maps.Components.Things;
using Helion.Resource;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Container.Linkable;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using Helion.Worlds.Entities.Definition;
using Helion.Worlds.Entities.Definition.Composer;
using Helion.Worlds.Entities.Inventories;
using Helion.Worlds.Entities.Players;
using Helion.Worlds.Entities.Spawn;
using Helion.Worlds.Geometry.Sectors;
using Helion.Worlds.Sound;
using MoreLinq.Extensions;
using NLog;

namespace Helion.Worlds.Entities
{
    public class EntityManager : IDisposable
    {
        public const int NoTid = 0;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly LinkableList<Entity> Entities = new();
        public readonly SpawnLocations SpawnLocations = new();
        public readonly World World;
        public readonly EntityDefinitionComposer DefinitionComposer;
        public readonly List<Player> Players = new();
        private readonly SoundManager m_soundManager;
        private readonly AvailableIndexTracker m_entityIdTracker = new();
        private readonly Dictionary<int, ISet<Entity>> TidToEntity = new();
        private readonly SkillLevel m_skill;

        public EntityManager(World world, Resources resources, SoundManager soundManager,
            SkillLevel skill)
        {
            World = world;
            m_soundManager = soundManager;
            DefinitionComposer = new EntityDefinitionComposer(resources);
            m_skill = skill;
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
            int id = m_entityIdTracker.Next();
            Sector sector = World.BspTree.ToSector(position);
            position.Z = GetPositionZ(sector, in position, zHeight);
            Entity entity = new(id, tid, definition, position, angle, sector, this, m_soundManager, World);

            // This only needs to happen on map population
            if (init && !ZHeightSet(zHeight))
            {
                entity.SetZ(entity.Sector.ToFloorZ(position), false);
                entity.PrevPosition = entity.Position;
            }

            FinishCreatingEntity(entity);
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

        public Player CreatePlayer(int playerIndex, Player? existingPlayer = null)
        {
            EntityDefinition? playerDefinition = DefinitionComposer.GetByName(Constants.PlayerClass);
            if (playerDefinition == null)
            {
                Log.Error("Missing player definition class {0}, cannot create player {1}", Constants.PlayerClass, playerIndex);
                throw new HelionException("Missing the default player class, should never happen");
            }

            Entity? spawnSpot = SpawnLocations.GetPlayerSpawn(playerIndex);
            if (spawnSpot == null)
            {
                Log.Warn("No player {0} spawns found, creating player at origin", playerIndex);
                return CreatePlayerEntity(playerIndex, playerDefinition, Vec3D.Zero, 0.0, 0.0);
            }

            Player player = CreatePlayerEntity(playerIndex, playerDefinition, spawnSpot.Position, 0.0, spawnSpot.AngleRadians);
            if (existingPlayer != null)
            {
                player.CopyProperties(existingPlayer);
                player.Inventory.ClearKeys();
            }
            else
            {
                SetDefaultInventory(player);
            }

            Players.Add(player);
            return player;
        }

        private void SetDefaultInventory(Player player)
        {
            GiveWeapon(player, "FIST");
            GiveWeapon(player, "PISTOL");

            GiveAmmo(player, "CLIP", 50);

            var weapon = player.Inventory.Weapons.GetWeapon("PISTOL");
            if (weapon != null)
                player.ChangeWeapon(weapon);
        }

        private void GiveAmmo(Player player, string name, int amount)
        {
            var ammo = DefinitionComposer.GetByName(name);
            if (ammo != null)
                player.Inventory.Add(ammo, 50);
        }

        private void GiveWeapon(Player player, string name)
        {
            var weapon = DefinitionComposer.GetByName(name);
            if (weapon != null)
                player.GiveWeapon(weapon, false);
        }

        public void PopulateFrom(Map map)
        {
            List<Entity> relinkEntities = new();

            foreach (Thing mapThing in map.Things)
            {
                if (!ShouldSpawn(mapThing, m_skill))
                    continue;

                EntityDefinition? definition = DefinitionComposer.GetByID(mapThing.EditorNumber);
                if (definition == null)
                {
                    Log.Warn("Cannot find entity by editor number {0} at {1}", mapThing.EditorNumber, mapThing.Position.To2D());
                    continue;
                }

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

        private static bool ShouldSpawn(Thing mapThing, SkillLevel skill)
        {
            // TODO: These should be offloaded into SinglePlayerWorld...
            if (!mapThing.Flags.SinglePlayer)
                return false;

            switch (skill)
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

        private void FinishCreatingEntity(Entity entity)
        {
            LinkableNode<Entity> node = Entities.Add(entity);
            entity.EntityListNode = node;

            World.Link(entity);

            if (entity.Flags.SpawnCeiling)
            {
                double offset = ZHeightSet(entity.Position.Z) ? -entity.Position.Z : 0;
                entity.SetZ(entity.Sector.ToCeilingZ(entity.Position) - entity.Height + offset, false);
            }

            entity.ResetInterpolation();
            entity.SetSpawnState();
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
            int id = m_entityIdTracker.Next();
            Sector sector = World.BspTree.ToSector(position);
            position.Z = GetPositionZ(sector, position, zHeight);
            Player player = new Player(id, 0, definition, position, angle, sector, this, m_soundManager, World, playerNumber);

            var armor = DefinitionComposer.GetByName(Inventory.ArmorClassName);
            if (armor != null)
                player.Inventory.Add(armor, 0);

            FinishCreatingEntity(player);

            return player;
        }

        public void Dispose()
        {
            Entities.ForEach(entity => entity.Dispose());
        }
    }
}