using Helion.Geometry.Vectors;
using Helion.Maps;
using Helion.Maps.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Models;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Players;
using Helion.World.Entities.Spawn;
using Helion.World.Geometry.Sectors;
using Helion.World.Stats;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.World.Entities;

public class EntityManager : IDisposable
{
    public record struct EntityModelPair(EntityModel Model, Entity Entity);

    public class WorldModelPopulateResult(List<Player> players, Dictionary<int, EntityModelPair> entities)
    {
        public List<Player> Players = players;
        public Dictionary<int, EntityModelPair> Entities = entities;
    }

    public const int NoTid = 0;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public int EntityCount;
    public Entity? Head;
    public LinkedList<Entity> TeleportSpots = new();
    public SpawnLocations SpawnLocations;
    public IWorld World;    

    public EntityDefinitionComposer DefinitionComposer;
    public List<Player> Players = [];
    public List<Player> RemovedPlayers = [];
    public List<Player> VoodooDolls = [];
    public List<Entity> MusicChangers = [];
    private readonly LookupArray<Player?> RealPlayersByNumber = new();
    private readonly Dictionary<int, LinkedList<Entity>> TidToEntity = [];

    public EntityManager(IWorld world)
    {
        World = world;
        SpawnLocations = new SpawnLocations(world);
        DefinitionComposer = world.ArchiveCollection.EntityDefinitionComposer;
    }

    private static bool ZHeightSet(double z)
    {
        return z != double.MinValue && z != 0.0;
    }

    private static readonly LinkedList<Entity> EmptyLinkedList = new();

    public LinkedList<Entity> FindByTid(int tid)
    {
        return TidToEntity.TryGetValue(tid, out var entities) ? entities : EmptyLinkedList;
    }

    public Entity? FindById(int id)
    {
        var entity = Head;
        while (entity != null)
        {
            if (entity.Id == id)
                return entity;
            entity = entity.Next;
        }
        return null;
    }

    public Entity? Create(string className, in Vec3D pos, bool initSpawn = false)
    {
        var def = DefinitionComposer.GetByName(className);
        return def != null ? Create(def, pos, 0.0, 0.0, 0, default, initSpawn: initSpawn) : null;
    }

    public Entity Create(EntityDefinition definition, Vec3D position, double zHeight, double angle, int tid, in SpecialArgs args, bool initSpawn = false, int editorId = -1)
    {
        EntityCount++;
        var sector = World.ToSubsector(position.X, position.Y).Sector;

        position.Z = GetPositionZ(sector, in position, zHeight);
        var entity = 
            definition.Type == EntityType.AmbientSound ? 
            CreateAmbientSound(World.DataCache.EntityId++, tid, definition, position, angle, sector, World, editorId, args) :
            World.DataCache.GetEntity(tid, definition, position, angle, sector, World, args);

        if (entity.Definition.Properties.FastSpeed > 0 && World.IsFastMonsters)
            entity.Properties.MonsterMovementSpeed = entity.Definition.Properties.FastSpeed;

        // This only needs to happen on map population
        if (initSpawn && !ZHeightSet(zHeight))
        {
            entity.Position.Z = entity.Sector.ToFloorZ(position);
            entity.PrevPosition = entity.Position;
        }

        FinishCreatingEntity(entity, zHeight, false, true, initSpawn);
        return entity;
    }

    private AmbientSound CreateAmbientSound(int id, int thingId, EntityDefinition definition, Vec3D position, double angle, Sector sector, IWorld world, int editorId, SpecialArgs args)
    {
        var index = args.Arg0;
        if (editorId < (int)EditorId.AmbientSoundEnd)
            index = editorId - (int)EditorId.AmbientSoundStart + 1;
        
        args.Arg0 = index;
        var entity = new AmbientSound();
        entity.Set(-1, id, thingId, definition, position, angle, sector, world, args);

        World.ArchiveCollection.SoundInfo.TryGetAmbientSound(index, out entity.AmbientSoundInfo);
        return entity;
    }

    public void Destroy(Entity entity, bool removeFromIdList = true)
    {
        if (entity.IsDisposed)
            return;

        EntityCount--;

        if (removeFromIdList && TidToEntity.TryGetValue(entity.ThingId, out var entities))
        {
            var node = entities.Find(entity);
            if (node != null)
            {
                World.DataCache.FreeLinkedListNodeEntity(node);
                entities.Remove(node);
            }
        }

        if (entity.Flags.IsTeleportSpot())
            TeleportSpots.Remove(entity);

        if (entity.PlayerObj != null)
            Players.Remove(entity.PlayerObj);

        entity.Dispose();
    }

    public void Destroy(LinkedList<Entity> entities)
    {
        for (var node = entities.First; node != null; node = node.Next)
        {
            Destroy(node.Value, false);
            World.DataCache.FreeLinkedListNodeEntity(node);
        }

        entities.Clear();
    }

    public Player RespawnPlayer(int playerIndex, Entity spawnSpot) =>
        CreatePlayer(playerIndex, spawnSpot, CreatePlayerOptions.Respawn);

    public Player CreatePlayer(int playerIndex, Entity spawnSpot, CreatePlayerOptions options = CreatePlayerOptions.None)
    {
        Player player;
        EntityDefinition? playerDefinition = DefinitionComposer.GetByName(Constants.PlayerClass);
        if (playerDefinition == null)
        {
            Log.Error("Missing player definition class {0}, cannot create player {1}", Constants.PlayerClass, playerIndex);
            throw new HelionException("Missing the default player class, should never happen");
        }
                
        var addedPlayer = Players.Count <= playerIndex;
        if (!addedPlayer && (options & CreatePlayerOptions.Respawn) != 0)
            RemovedPlayers.Add(Players[playerIndex]);

        var isVoodooDoll = (options & CreatePlayerOptions.VooDooDoll) != 0;
        player = CreatePlayerEntity(playerIndex, playerDefinition, spawnSpot.Position, 0.0, spawnSpot.AngleRadians);
        player.IsVooDooDoll = isVoodooDoll;

        if (isVoodooDoll)
        {
            VoodooDolls.Add(player);
            return player;
        }

        if (addedPlayer)
        {
            AddRealPlayer(player);
        }
        else
        {
            Players[playerIndex] = player;
            RealPlayersByNumber.Set(player.PlayerNumber, player);
        }

        return player;
    }

    public CameraPlayer CreateCameraPlayer(Entity spawnSpot)
    {
        EntityDefinition? playerDefinition = DefinitionComposer.GetByName(Constants.PlayerClass);
        if (playerDefinition == null)
        {
            Log.Error("Missing player definition class {0}, cannot create player", Constants.PlayerClass);
            throw new HelionException("Missing the default player class, should never happen");
        }

        Vec3D position = spawnSpot.Position;
        Sector sector = World.ToSubsector(position.X, position.Y).Sector;
        CameraPlayer player = new(0, playerDefinition, position, spawnSpot.AngleRadians, sector, World);
        return player;
    }

    private void AddRealPlayer(Player player)
    {
        RealPlayersByNumber.Set(player.PlayerNumber, player);
        Players.Add(player);
    }

    public void PopulateFrom(IMap map, LevelStats levelStats)
    {
        List<Entity> relinkEntities = [];
        var things = map.GetThings();
        World.DataCache.SetEntitiesForMapLoad(things.Count);

        foreach (IThing mapThing in things)
        {
            if (!ShouldSpawn(mapThing))
                continue;

            // Boom ports appear to ignore 14100
            if (mapThing.EditorNumber == (int)EditorId.MusicChangerStart)
                continue;

            var isMusicChanger = EditorIds.IsMusicChanger(mapThing.EditorNumber);
            var isAmbientSound = EditorIds.IsAmbientSound(mapThing.EditorNumber);
            EntityDefinition? definition;

            if (isMusicChanger)
                definition = DefinitionComposer.GetByName(Constants.MusicChanger);
            else if (isAmbientSound)
                definition = DefinitionComposer.GetByName(Constants.AmbientSound);
            else
                definition = DefinitionComposer.GetByID(mapThing.EditorNumber);

            if (definition == null)
            {
                 Log.Warn("Cannot find entity by editor number {0} at {1}", mapThing.EditorNumber, mapThing.Position.XY);
                continue;
            }

            if (!definition.SpawnState.HasValue)
                continue;

            if (World.Config.Game.NoMonsters && definition.Flags.CountKill())
                continue;

            if (definition.Flags.CountKill() && !definition.Flags.Friendly())
                levelStats.TotalMonsters++;
            if (definition.Flags.CountItem())
                levelStats.TotalItems++;

            var angleRadians = MathHelper.ToRadians(mapThing.Angle);
            var position = mapThing.Position;
            // position.Z is the potential zHeight variable, not the actual z position. We need to pass it to Create to ensure the zHeight is set
            var entity = Create(definition, position, position.Z, angleRadians, mapThing.ThingId, mapThing.Args, initSpawn: true, editorId: mapThing.EditorNumber);
            entity.Special = mapThing.Special;
            entity.Gravity = mapThing.Gravity;

            if (mapThing.Alpha.HasValue)
                entity.Alpha = mapThing.Alpha.Value;

            if (mapThing.Flags.Ambush)
                entity.Flags.SetAmbush(mapThing.Flags.Ambush);
            if (mapThing.Flags.Friendly)
                entity.Flags.SetFriendly(mapThing.Flags.Friendly);
            if (mapThing.Flags.Invisible)
                entity.Flags.SetInvisible(mapThing.Flags.Invisible);
            if (mapThing.Flags.CountKill)
                entity.Flags.SetCountKill(mapThing.Flags.CountKill);
            if (mapThing.Flags.CountItem)
                entity.Flags.SetCountItem(mapThing.Flags.CountItem);
            if (mapThing.Flags.Dormant)
                entity.Flags.SetDormant(mapThing.Flags.Dormant);
            if (mapThing.Health.HasValue)
                entity.Health = mapThing.Health.Value;

            if (mapThing.Flags.CountSecret)
            {
                entity.Flags.SetCountSecret(mapThing.Flags.CountSecret);
                levelStats.TotalSecrets++;
            }

            if (entity.FrameState.Frame.Ticks > 0)
                entity.FrameState.SetTics((World.Random.NextByte() % entity.FrameState.Frame.Ticks) + 1);

            if (!entity.Flags.ActLikeBridge() && ZHeightSet(position.Z))
                relinkEntities.Add(entity);

            if (isMusicChanger)
                entity.ThingId = mapThing.EditorNumber - (int)EditorId.MusicChangerStart;
        }

        //Relink entities with a z-height only, this way they can properly stack with other things in the map now that everything exists
        for (int i = 0; i < relinkEntities.Count; i++)
        {
            var relink = relinkEntities[i];
            relink.UnlinkFromWorld();
            World.Link(relink);
            relink.PrevPosition = relinkEntities[i].Position;
        }
    }

    public WorldModelPopulateResult PopulateFrom(WorldModel worldModel)
    {
        var maxEntityId = 0;
        World.DataCache.SetEntitiesForMapLoad(worldModel.Entities.Count + worldModel.Players.Count);
        List<Player> players = new(worldModel.Players.Count);
        Dictionary<int, EntityModelPair> entities = new(worldModel.Entities.Count + worldModel.Players.Count);

        // Entities are serialized backwards because of the linked list implementation
        for (int i = worldModel.Entities.Count - 1; i >= 0; i--)
        {
            var entityModel = worldModel.Entities[i];
            if (entityModel.Id > maxEntityId)
                maxEntityId = entityModel.Id;
            var definition = DefinitionComposer.GetByName(entityModel.Name);
            if (definition == null)
            {
                Log.Error($"Failed to find entity definition for:{entityModel.Name}");
                continue;
            }

            Entity entity;
            if (definition.Type == EntityType.AmbientSound)
            {
                entity = CreateAmbientSound(entityModel.Id, entityModel.ThingId, definition, default, 0, Sector.Default, World, (int)EditorId.AmbientSoundEnd, entityModel.Args);
                entity.Set(-1, entityModel, definition, World);

            }
            else
            {
                int index = World.DataCache.EntityLength++;
                entity = World.DataCache.Entities[index];
                entity.Set(index, entityModel, definition, World);
            }

            AddEntityToList(entity);
            entities.Add(entityModel.Id, new(entityModel, entity));
        }

        for (int i = 0; i < worldModel.Players.Count; i++)
        {
            var playerModel = worldModel.Players[i];
            if (playerModel.Id > maxEntityId)
                maxEntityId = playerModel.Id;
            bool isVoodooDoll = players.Any(x => x.PlayerNumber == playerModel.Number);
            Player? player = CreatePlayerFromModel(playerModel, entities, isVoodooDoll);
            if (player == null)
            {
                Log.Error($"Failed to create player {playerModel.Name}.");
                continue;
            }
            players.Add(player);
            player.SpawnPoint.X = playerModel.SpawnPointX;
            player.SpawnPoint.Y = playerModel.SpawnPointY;
            player.SpawnPoint.Z = playerModel.SpawnPointZ;
        }

        for (int i = 0; i < worldModel.Entities.Count; i++)
        {
            var entityModel = worldModel.Entities[i];
            if (!entities.TryGetValue(entityModel.Id, out var entity))
                continue;

            if (entityModel.Owner.HasValue)
            {
                if (entities.TryGetValue(entityModel.Owner.Value, out var entityOwner))
                    entity.Entity.SetOwner(entityOwner.Entity);
            }

            if (entityModel.Target.HasValue)
            {
                if (entities.TryGetValue(entityModel.Target.Value, out var entityTarget))
                    entity.Entity.SetTarget(entityTarget.Entity);
            }

            if (entityModel.Tracer.HasValue)
            {
                if (entities.TryGetValue(entityModel.Tracer.Value, out var tracerTarget))
                    entity.Entity.SetTracer(tracerTarget.Entity);
            }

            entity.Entity.SpawnPoint.X = entity.Model.SpawnPointX;
            entity.Entity.SpawnPoint.Y = entity.Model.SpawnPointY;
            entity.Entity.SpawnPoint.Z = entity.Model.SpawnPointZ;
        }

        EntityCount = worldModel.Entities.Count;
        World.DataCache.EntityId = maxEntityId + 1;
        return new WorldModelPopulateResult(players, entities);
    }

    private void AddEntityToList(Entity entity)
    {
        if (Head == null)
        {
            Head = entity;
            return;
        }

        entity.Next = Head;
        Head.Previous = entity;
        Head = entity;
    }

    public void FinalizeFromWorldLoad(WorldModelPopulateResult result)
    {
        for (var entity = Head; entity != null; entity = entity.Next)
        {
            World.Link(entity);
            bool? setOnGround = null;

            if (result.Entities.TryGetValue(entity.Id, out var pair))
            {
                entity.HighestFloorSector = GetValidSector(World, entity.Sector, pair.Model.HighSec);
                entity.LowestCeilingSector = GetValidSector(World, entity.Sector, pair.Model.LowSec);
                entity.HighestFloorZ = entity.HighestFloorSector.ToFloorZ(entity.Position);
                entity.LowestCeilingZ = entity.LowestCeilingSector.ToCeilingZ(entity.Position);

                entity.HighestFloorObject = GetBoundingObject(result, entity.HighestFloorSector, pair.Model.HighEntity, out var highFloorEntity);
                entity.LowestCeilingObject = GetBoundingObject(result, entity.LowestCeilingSector, pair.Model.LowEntity, out var lowCeilEntity);
                if (highFloorEntity != null)
                    entity.SetHighestFloorEntity(highFloorEntity);
                if (lowCeilEntity != null)
                    entity.SetLowestCeilingEntity(lowCeilEntity);

                entity.Position = new Vec3D(pair.Model.Box.CenterX, pair.Model.Box.CenterY, pair.Model.Box.CenterZ);
                setOnGround = pair.Model.OnGround;
            }

            PostProcessEntity(entity);
            FinalizeEntity(entity, false, initSpawn: false);
            if (setOnGround != null)
                entity.OnGround = setOnGround.Value;

            if (entity.Definition.Type == EntityType.MusicChanger)
                MusicChangers.Add(entity);
        }

        // The linked list is backwards so the starts have to be reversed
        SpawnLocations.ReversePlayerStarts();
    }

    public Player? GetRealPlayer(int playerNumber)
    {
        RealPlayersByNumber.TryGetValue(playerNumber, out var player);
        return player;
    }

    private IFloorCeilingAnchor GetBoundingObject(WorldModelPopulateResult result, Sector sector, int? entityId, out Entity? entity)
    {
        entity = null;
        if (!entityId.HasValue)
            return sector;

        if ((entityId & EntityModel.Sector3DEntityFlag) != 0)
        {
            int sectorId = entityId.Value & ~EntityModel.Sector3DEntityFlag;
            if (World.SpecialManager.Sectors3D.TryGetValue(sectorId, out var sector3D))
            {
                entity = sector3D.GetSectorEntity3D();
                return entity;
            }

            return sector;
        }

        if ((entityId & EntityModel.MidTexEntityFlag) != 0)
        {
            int lineId = entityId.Value & ~EntityModel.MidTexEntityFlag;
            if (!World.IsLineIdValid(lineId))
                return sector;

            var line = World.Lines[lineId];
            if (!line.Flags.Blocking.MidTex3D)
                return sector;

            entity = World.Lines[lineId].GetMidTexEntity(World);
            return entity;
        }

        if (!result.Entities.TryGetValue(entityId.Value, out var pair))
            return sector;

        entity = pair.Entity;
        return pair.Entity;
    }

    private static Sector GetValidSector(IWorld world, Sector sector, int? id)
    {
        if (!id.HasValue || !world.IsSectorIdValid(id.Value))
            return sector;

        return world.Sectors[id.Value];
    }

    private Player? CreatePlayerFromModel(PlayerModel playerModel, Dictionary<int, EntityModelPair> entities, bool isVoodooDoll)
    {
        var playerDefinition = DefinitionComposer.GetByName(playerModel.Name);
        if (playerDefinition != null)
        {
            var player = new Player();
            int index = World.DataCache.EntityLength++;
            World.DataCache.Entities[index] = player;
            player.Set(index, playerModel, entities, playerDefinition, World);
            player.IsVooDooDoll = isVoodooDoll;

            AddEntityToList(player);
            entities.Add(player.Id, new(playerModel, player));

            if (isVoodooDoll)
            {
                VoodooDolls.Add(player);
                return player;
            }

            AddRealPlayer(player);
            return player;
        }

        return null;
    }

    private bool ShouldSpawn(IThing mapThing)
    {
        // Ignore difficulty on spawns...
        if ((mapThing.EditorNumber > 0 && mapThing.EditorNumber < 5) || mapThing.EditorNumber == 1)
            return true;

        if (!World.ShouldSpawn(mapThing))
            return false;

        return (SkillLevel)World.SkillDefinition.SpawnFilter switch
        {
            SkillLevel.VeryEasy => mapThing.Flags.Skill1,
            SkillLevel.Easy => mapThing.Flags.Skill2,
            SkillLevel.Medium => mapThing.Flags.Skill3,
            SkillLevel.Hard => mapThing.Flags.Skill4,
            SkillLevel.Nightmare => mapThing.Flags.Skill5,
            _ => false,
        };
    }

    private static double GetPositionZ(Sector sector, in Vec3D position, double zHeight)
    {
        if (ZHeightSet(zHeight))
            return zHeight + sector.ToFloorZ(position);

        return position.Z;
    }

    private static void FinalizeEntity(Entity entity, bool checkOnGround, double zHeight = 0, bool initSpawn = true)
    {
        if (initSpawn && entity.Flags.SpawnCeiling())
        {
            // Need to always use Doom's old height here.
            double height = entity.GetClampHeight();
            double offset = ZHeightSet(zHeight) ? -zHeight : 0;
            entity.Position.Z = entity.Sector.ToCeilingZ(entity.Position) - height + offset;
        }

        if (checkOnGround)
            entity.CheckOnGround();
        entity.ResetInterpolation();
    }

    private void FinishCreatingEntity(Entity entity, double zHeight, bool clamp, bool checkOnGround, bool initSpawn)
    {
        AddEntityToList(entity);

        if (clamp)
            World.LinkClamped(entity);
        else
            World.Link(entity);

        FinalizeEntity(entity, checkOnGround, zHeight, initSpawn);
                
       entity.SpawnPoint = entity.Position;
        // Vanilla did not execute action functions on creation, it just set the state
        // Action functions will not execute until Tick() is called
        if (entity.Definition.SpawnState != null)
            entity.FrameState.SetFrameIndexNoAction(entity, entity.Definition.SpawnState.Value);

        if (entity.Definition.Flags.CountKill() || entity.Definition.Flags.IsMonster())
            entity.Health = Math.Max((int)(entity.Health * World.SkillDefinition.MonsterHealthFactor), 1);

        if (entity.Definition.Type == EntityType.MusicChanger)
            MusicChangers.Add(entity);

        if (WorldStatic.MirrorCorpse && !entity.Flags.DontMirrorCorpse() && 
            (entity.Definition.Type == EntityType.BulletPuff || entity.Definition.Type == EntityType.Blood || entity.Flags.Corpse())
            && (World.SecondaryRandom.NextByte() & 1) != 0)
        {
            entity.Flags.FlipMirror();
        }

        PostProcessEntity(entity);
    }

    private void PostProcessEntity(Entity entity)
    {
        SpawnLocations.AddPossibleSpawnLocation(entity);

        if (entity.ThingId != NoTid)
        {
            if (TidToEntity.TryGetValue(entity.ThingId, out var entities))
            {
                entities.AddLast(entity);
            }
            else
            {
                var list = new LinkedList<Entity>();
                list.AddLast(World.DataCache.GetLinkedListNodeEntity(entity));
                TidToEntity.Add(entity.ThingId, list);
            }
        }

        if (entity.Flags.IsTeleportSpot())
            TeleportSpots.AddLast(entity);
    }

    private Player CreatePlayerEntity(int playerNumber, EntityDefinition definition, Vec3D position, double zHeight, double angle)
    {
        Sector sector = World.ToSubsector(position.X, position.Y).Sector;
        position.Z = GetPositionZ(sector, position, zHeight);
        var player = World.DataCache.GetPlayer(0, definition, position, angle, sector, World, playerNumber);

        var armor = DefinitionComposer.GetByName(Inventory.ArmorClassName);
        if (armor != null)
            player.Inventory.Add(armor, 0);

        FinishCreatingEntity(player, zHeight, false, true, true);
        return player;
    }

    public void Dispose()
    {
        ClearEntities();
        GC.SuppressFinalize(this);
    }

    public void UpdateTo(IWorld world)
    {
        World = world;
        ClearEntities();
        SpawnLocations.Clear();
        TidToEntity.Clear();
        Players.Clear();
        RemovedPlayers.Clear();
        VoodooDolls.Clear();
        MusicChangers.Clear();
        RealPlayersByNumber.SetAll(null);
        TeleportSpots.Clear();
    }

    private void ClearEntities()
    {
        var entity = Head;
        Entity? nextEntity;
        while (entity != null)
        {
            nextEntity = entity.Next;
            entity.Dispose();
            entity = nextEntity;
        }
    }
}
