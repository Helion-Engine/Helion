using System;
using System.Collections.Generic;
using Helion.Audio;
using Helion.Maps;
using Helion.Maps.Specials.Compatibility;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Locks;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using Helion.Util.RandomGenerators;
using Helion.World.Blockmap;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Entities.Definition.Properties.Components;
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;
using Helion.World.Physics.Blockmap;
using Helion.World.Sound;
using Helion.World.Special;
using MoreLinq;
using NLog;
using static Helion.Util.Assertion.Assert;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Container;
using Helion.World.Entities.Definition;
using Helion.Models;
using Helion.Util.Timing;
using Helion.World.Entities.Definition.Flags;
using System.Linq;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.World.Cheats;
using Helion.World.Stats;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Util;
using Helion.Resources.IWad;
using Helion.Dehacked;
using Helion.Resources.Archives;
using Helion.Util.Profiling;
using Helion.World.Entities.Inventories;
using Helion.Maps.Specials;
using Helion.World.Entities.Definition.States;
using System.Diagnostics;
using Helion.World.Special.Specials;
using System.Diagnostics.CodeAnalysis;
using Helion.Demo;

namespace Helion.World;

public abstract partial class WorldBase : IWorld
{
    private const double MaxPitch = 80.0 * Math.PI / 180.0;
    private const double MinPitch = -80.0 * Math.PI / 180.0;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Fires when an entity activates a line special with use or by crossing a line.
    /// </summary>
    public event EventHandler<EntityActivateSpecialEventArgs>? EntityActivatedSpecial;
    public event EventHandler<LevelChangeEvent>? LevelExit;
    public event EventHandler? WorldResumed;

    public readonly long CreationTimeNanos;
    public string MapName { get; protected set; }
    public readonly BlockMap Blockmap;
    public WorldState WorldState { get; protected set; } = WorldState.Normal;
    public int Gametick { get; private set; }
    public int LevelTime { get; private set; }
    public double Gravity { get; private set; } = 1.0;
    public bool Paused { get; private set; }
    public bool PlayingDemo { get; set; }
    public bool DemoEnded { get; set; }
    public IRandom Random => m_random;
    public IRandom SecondaryRandom { get; private set; }
    public IList<Line> Lines => Geometry.Lines;
    public IList<Side> Sides => Geometry.Sides;
    public IList<Wall> Walls => Geometry.Walls;
    public IList<Sector> Sectors => Geometry.Sectors;
    public BspTree BspTree => Geometry.BspTree;
    public LinkableList<Entity> Entities => EntityManager.Entities;
    public EntityManager EntityManager { get; }
    public WorldSoundManager SoundManager { get; }
    public abstract Vec3D ListenerPosition { get; }
    public abstract double ListenerAngle { get; }
    public abstract double ListenerPitch { get; }
    public abstract Entity ListenerEntity { get; }
    public BlockmapTraverser BlockmapTraverser => PhysicsManager.BlockmapTraverser;
    public SpecialManager SpecialManager { get; private set; }
    public IConfig Config { get; private set; }
    public MapInfoDef MapInfo { get; private set; }
    public LevelStats LevelStats { get; } = new();
    public SkillDef SkillDefinition { get; private set; }
    public ArchiveCollection ArchiveCollection { get; protected set; }
    public GlobalData GlobalData { get; }
    public CheatManager CheatManager { get; } = new();
    public DataCache DataCache => ArchiveCollection.DataCache;

    public GameInfoDef GameInfo => ArchiveCollection.Definitions.MapInfoDefinition.GameDefinition;
    public TextureManager TextureManager => ArchiveCollection.TextureManager;

    protected readonly IAudioSystem AudioSystem;
    protected readonly MapGeometry Geometry;
    protected readonly PhysicsManager PhysicsManager;
    protected readonly IMap Map;
    protected readonly Profiler Profiler;
    private IRandom m_random;
    private IRandom m_saveRandom;

    private int m_exitTicks;
    private int m_easyBossBrain;
    private int m_soundCount;
    private int m_lastBumpActivateGametick = 0;
    private LevelChangeType m_levelChangeType = LevelChangeType.Next;
    private Entity[] m_bossBrainTargets = Array.Empty<Entity>();
    private readonly List<MonsterCountSpecial> m_bossDeathSpecials = new();

    protected WorldBase(GlobalData globalData, IConfig config, ArchiveCollection archiveCollection,
        IAudioSystem audioSystem, Profiler profiler, MapGeometry geometry, MapInfoDef mapInfoDef,
        SkillDef skillDef, IMap map, WorldModel? worldModel = null, IRandom? random = null)
    {
        m_random = random ?? new DoomRandom();
        m_saveRandom = m_random;
        SecondaryRandom = m_random.Clone();

        CreationTimeNanos = Ticker.NanoTime();
        GlobalData = globalData;
        ArchiveCollection = archiveCollection;
        AudioSystem = audioSystem;
        Config = config;
        MapInfo = mapInfoDef;
        SkillDefinition = skillDef;
        MapName = map.Name;
        Profiler = profiler;
        Geometry = geometry;
        Map = map;
        Blockmap = new BlockMap(Lines);
        SoundManager = new WorldSoundManager(this, audioSystem);
        EntityManager = new EntityManager(this);
        PhysicsManager = new PhysicsManager(this, BspTree, Blockmap, m_random);
        SpecialManager = new SpecialManager(this, m_random);

        if (worldModel != null)
        {
            WorldState = worldModel.WorldState;
            Gametick = worldModel.Gametick;
            LevelTime = worldModel.LevelTime;
            m_soundCount = worldModel.SoundCount;
            Gravity = worldModel.Gravity;
            Random.Clone(worldModel.RandomIndex);
            CurrentBossTarget = worldModel.CurrentBossTarget;
            GlobalData = new()
            {
                VisitedMaps = GetVisitedMaps(worldModel.VisitedMaps),
                TotalTime = worldModel.TotalTime
            };

            LevelStats.TotalMonsters = worldModel.TotalMonsters;
            LevelStats.TotalItems = worldModel.TotalItems;
            LevelStats.TotalSecrets = worldModel.TotalSecrets;
            LevelStats.KillCount = worldModel.KillCount;
            LevelStats.ItemCount = worldModel.ItemCount;
            LevelStats.SecretCount = worldModel.SecretCount;
        }
    }

    private void DemoPlaybackEnded(object? sender, EventArgs e)
    {
        Paused = true;
    }

    private IList<MapInfoDef> GetVisitedMaps(IList<string> visitedMaps)
    {
        List<MapInfoDef> maps = new();
        foreach (string mapName in visitedMaps)
        {
            var mapInfoDef = ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.GetMap(mapName);
            if (mapInfoDef != null)
                maps.Add(mapInfoDef);
        }

        return maps;
    }

    ~WorldBase()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public void SetRandom(IRandom random) => m_random = random;

    public virtual void Start(WorldModel? worldModel)
    {
        AddMapSpecial();
        InitBossBrainTargets();

        if (worldModel == null)
            SpecialManager.StartInitSpecials(LevelStats);
    }

    public Player? GetLineOfSightPlayer(Entity entity, bool allaround)
    {
        for (int i = 0; i < EntityManager.Players.Count; i++)
        {
            Player player = EntityManager.Players[i];
            if (player.IsDead)
                continue;

            if (!allaround && !InFieldOfViewOrInMeleeDistance(entity, player))
                continue;

            if (CheckLineOfSight(entity, player))
                return player;
        }

        return null;
    }

    public Entity? GetLineOfSightEnemy(Entity entity, bool allaround)
    {
        Box2D box = new(entity.Position.XY, 1280);
        List<BlockmapIntersect> intersections = BlockmapTraverser.GetBlockmapIntersections(box, BlockmapTraverseFlags.Entities,
            BlockmapTraverseEntityFlags.Solid | BlockmapTraverseEntityFlags.Shootable);
        for (int i = 0; i < intersections.Count; i++)
        {
            Entity? checkEntity = intersections[i].Entity;
            if (checkEntity == null)
                continue;

            if (ReferenceEquals(entity, checkEntity) || checkEntity.IsDead || entity.Flags.Friendly == checkEntity.Flags.Friendly || checkEntity.IsPlayer)
                continue;

            if (!allaround && !InFieldOfViewOrInMeleeDistance(entity, checkEntity))
                continue;

            if (CheckLineOfSight(entity, checkEntity))
                return checkEntity;
        }

        return null;
    }

    public double GetMoveFactor(Entity entity) => 
        PhysicsManager.GetMoveFactor(entity);

    public void NoiseAlert(Entity target, Entity source)
    {
        m_soundCount++;
        RecursiveSound(target, source.Sector, 0);
    }

    public void RecursiveSound(Entity target, Sector sector, int block)
    {
        if (sector.SoundValidationCount == m_soundCount && sector.SoundBlock <= block + 1)
            return;

        sector.SoundValidationCount = m_soundCount;
        sector.SoundBlock = block + 1;
        sector.SetSoundTarget(target);

        for (int i = 0; i < sector.Lines.Count; i++)
        {
            Line line = sector.Lines[i];
            if (line.Back == null || !LineOpening.IsOpen(line))
                continue;

            Sector other = line.Front.Sector == sector ? line.Back.Sector : line.Front.Sector;
            if (line.Flags.BlockSound)
            {
                // Has to cross two block sound lines to stop. This is how it was designed.
                if (block == 0)
                    RecursiveSound(target, other, 1);
            }
            else
            {
                RecursiveSound(target, other, block);
            }
        }
    }

    public void Link(Entity entity)
    {
        Precondition(entity.SectorNodes.Empty() && entity.BlockmapNodes.Empty(), "Forgot to unlink entity before linking");
        PhysicsManager.LinkToWorld(entity, null, false);
    }

    public void LinkClamped(Entity entity)
    {
        Precondition(entity.SectorNodes.Empty() && entity.BlockmapNodes.Empty(), "Forgot to unlink entity before linking");
        PhysicsManager.LinkToWorld(entity, null, true);
    }

    public virtual void Tick()
    {
        DebugCheck();

        if (Paused)
        {
            TickPlayerStatusBars();
            return;
        }

        Profiler.World.Total.Start();

        if (WorldState == WorldState.Exit)
        {
            SoundManager.Tick();
            m_exitTicks--;
            if (m_exitTicks == 0)
            {
                LevelChangeEvent changeEvent = new(m_levelChangeType);
                LevelExit?.Invoke(this, changeEvent);
                if (changeEvent.Cancel)
                    WorldState = WorldState.Normal;
                else
                    WorldState = WorldState.Exited;
                m_random = m_saveRandom;
            }
        }
        else if (WorldState == WorldState.Normal)
        {
            TickEntities();
            TickPlayers();
            SpecialManager.Tick();

            if (WorldState != WorldState.Exit)
            {
                ArchiveCollection.TextureManager.Tick();
                SoundManager.Tick();

                LevelTime++;
                GlobalData.TotalTime++;
            }
        }

        Gametick++;

        Profiler.World.Total.Stop();
    }

    private void TickPlayerStatusBars()
    {
        foreach (Player player in EntityManager.Players)
            player.StatusBar.Tick();
    }

    [Conditional("DEBUG")]
    private static void DebugCheck()
    {
        if (WeakEntity.Default.Entity != null)
            Fail("Static WeakEntity default reference was changed.");
    }

    private void TickEntities()
    {
        Profiler.World.TickEntity.Start();

        LinkableNode<Entity>? node = EntityManager.Entities.Head;
        while (node != null)
        {
            Entity entity = node.Value;
            entity.Tick();

            if (WorldState == WorldState.Exit)
                break;

            // Entities can be disposed after Tick() (rocket explosion, blood spatter etc.)
            if (!entity.IsDisposed)
            {
                PhysicsManager.Move(entity);

                if (entity.Respawn)
                    HandleRespawn(entity);

                if (entity.Sector.InstantKillEffect != InstantKillEffect.None && entity.OnSectorFloorZ(entity.Sector))
                    InstantKillSector(entity);
            }

            node = node.Next;
        }

        Profiler.World.TickEntity.Stop();
    }

    private void TickPlayers()
    {
        Profiler.World.TickPlayer.Start();

        foreach (Player player in EntityManager.Players)
        {
            if (WorldState == WorldState.Exit)
                break;

            // Doom did not apply sector damage to voodoo dolls
            if (player.IsVooDooDoll)
                continue;

            player.HandleTickCommand();
            player.TickCommand.TickHandled();

            if (player.Sector.SectorDamageSpecial != null)
                player.Sector.SectorDamageSpecial.Tick(player);

            if (player.Sector.Secret && player.OnSectorFloorZ(player.Sector))
            {
                DisplayMessage(player, null, "$SECRETMESSAGE");
                SoundManager.PlayStaticSound("misc/secret");
                player.Sector.SetSecret(false);
                LevelStats.SecretCount++;
                player.SecretsFound++;
            }

            if (player.Sector.InstantKillEffect != InstantKillEffect.None && player.OnSectorFloorZ(player.Sector))
                InstantKillSector(player);
        }

        Profiler.World.TickPlayer.Stop();
    }

    private void InstantKillSector(Entity entity)
    {
        if (entity.IsDead)
            return;

        InstantKillEffect effect = entity.Sector.InstantKillEffect;
        if (!entity.IsPlayer && effect.HasFlag(InstantKillEffect.KillMonsters))
        {
            entity.ForceGib();
            return;
        }

        if (entity.PlayerObj == null)
            return;

        Player player = entity.PlayerObj;
        if (effect.HasFlag(InstantKillEffect.KillAllPlayersExit))
        {
            KillAllPlayers();
            ExitLevel(LevelChangeType.Next);
        }
        if (effect.HasFlag(InstantKillEffect.KillAllPlayersSecretExit))
            {
            KillAllPlayers();
            ExitLevel(LevelChangeType.SecretNext);
        }
        if (effect.HasFlag(InstantKillEffect.KillUnprotectedPlayer) && !player.Flags.Invulnerable &&
            !player.Inventory.IsPowerupActive(PowerupType.IronFeet))
            player.ForceGib();
        if (effect.HasFlag(InstantKillEffect.KillPlayer))
            player.ForceGib();
    }

    private void KillAllPlayers()
    {
        foreach (var player in EntityManager.Players)
        {
            if (player.IsVooDooDoll)
                continue;

            player.ForceGib();
        }
    }

    public void Pause()
    {
        if (Paused)
            return;

        ResetInterpolation();
        SoundManager.Pause();

        Paused = true;
    }

    public void ResetInterpolation()
    {
        LinkableNode<Entity>? node = EntityManager.Entities.Head;
        while (node != null)
        {
            node.Value.ResetInterpolation();
            node = node.Next;
        }
        SpecialManager.ResetInterpolation();
    }

    public void Resume()
    {
        if (!Paused || DemoEnded)
            return;

        SoundManager.Resume();
        Paused = false;
        WorldResumed?.Invoke(this, EventArgs.Empty);
    }

    public void BossDeath(Entity entity)
    {
        if (!entity.Definition.EditorId.HasValue)
            return;

        if (EntityManager.Players.All(x => x.IsDead))
            return;

        foreach (var special in m_bossDeathSpecials)
        {
            if (special.EntityEditorId == entity.Definition.EditorId)
                special.Tick();
        }
    }

    private void AddMapSpecial()
    {
        switch (MapInfo.MapSpecial)
        {
            case MapSpecial.BaronSpecial:
                AddMonsterCountSpecial(m_bossDeathSpecials, (EntityFlags f) => f.E1M8Boss, 666, MapInfo.MapSpecialAction);
                break;
            case MapSpecial.CyberdemonSpecial:
                AddMonsterCountSpecial(m_bossDeathSpecials, (EntityFlags f) => f.E2M8Boss || f.E4M6Boss, 666, MapInfo.MapSpecialAction);
                break;
            case MapSpecial.SpiderMastermindSpecial:
                AddMonsterCountSpecial(m_bossDeathSpecials, (EntityFlags f) => f.E3M8Boss || f.E4M8Boss, 666, MapInfo.MapSpecialAction);
                break;
            case MapSpecial.Map07Special:
                AddMonsterCountSpecial(m_bossDeathSpecials, (EntityFlags f) => f.Map07Boss1, 666, MapSpecialAction.LowerFloor);
                AddMonsterCountSpecial(m_bossDeathSpecials, (EntityFlags f) => f.Map07Boss2, 667, MapSpecialAction.FloorRaiseByLowestTexture);
                break;
        }
    }

    private IEnumerable<EntityDefinition> GetEntityDefinitionsByFlag(Func<EntityFlags, bool> isMatch)
    {
        foreach (var def in EntityManager.DefinitionComposer.GetEntityDefinitions())
            if (isMatch(def.Flags))
                yield return def;
    }

    private void AddMonsterCountSpecial(List<MonsterCountSpecial> monsterCountSpecials, Func<EntityFlags, bool> isMatch, int sectorTag, 
        MapSpecialAction mapSpecialAction)
    {
        foreach (var def in GetEntityDefinitionsByFlag(isMatch))
            AddMonsterCountSpecial(monsterCountSpecials, def.Name, sectorTag, mapSpecialAction);
    }

    private void AddMonsterCountSpecial(List<MonsterCountSpecial> monsterCountSpecials, string monsterName, int sectorTag, MapSpecialAction mapSpecialAction)
    {
        EntityDefinition? definition = EntityManager.DefinitionComposer.GetByName(monsterName);
        if (definition == null || !definition.EditorId.HasValue)
        {
            Log.Error($"Failed to find {monsterName} for {mapSpecialAction}");
            return;
        }

        monsterCountSpecials.Add(new MonsterCountSpecial(this, SpecialManager, definition.EditorId.Value, sectorTag, mapSpecialAction));
    }

    private void InitBossBrainTargets()
    {
        List<Entity> targets = new();
        LinkableNode<Entity>? node = EntityManager.Entities.Head;
        while (node != null)
        {
            if (node.Value.Definition.Name.Equals("BOSSTARGET", StringComparison.OrdinalIgnoreCase))
                targets.Add(node.Value);
            node = node.Next;
        }

        // Doom chose for some reason to iterate in reverse order.
        targets.Reverse();
        m_bossBrainTargets = targets.ToArray();
    }

    public IEnumerable<Sector> FindBySectorTag(int tag) =>
        Geometry.FindBySectorTag(tag);

    public IEnumerable<Entity> FindByTid(int tid) =>
        EntityManager.FindByTid(tid);

    public IEnumerable<Line> FindByLineId(int lineId) =>
        Geometry.FindByLineId(lineId);

    public void SetLineId(Line line, int lineId) =>
        Geometry.SetLineId(line, lineId);

    public void Dispose()
    {
        PerformDispose();
        GC.SuppressFinalize(this);
    }

    public void ExitLevel(LevelChangeType type)
    {
        SoundManager.ClearSounds();
        m_levelChangeType = type;
        WorldState = WorldState.Exit;
        // The exit ticks thing is fudge. Change random to secondary to not break demos later.
        m_random = SecondaryRandom;
        m_exitTicks = 15;

        ResetInterpolation();
    }

    public Entity[] GetBossTargets()
    {
        m_easyBossBrain ^= 1;
        if (SkillDefinition.EasyBossBrain && m_easyBossBrain == 0)
            return Array.Empty<Entity>();

        return m_bossBrainTargets;
    }

    public int CurrentBossTarget { get; set; }

    public void TelefragBlockingEntities(Entity entity)
    {
        List<Entity> blockingEntities = entity.GetIntersectingEntities3D(entity.Position, BlockmapTraverseEntityFlags.Solid | BlockmapTraverseEntityFlags.Shootable);
        for (int i = 0; i < blockingEntities.Count; i++)
            blockingEntities[i].ForceGib();
    }

    /// <summary>
    /// Executes use logic on the entity. EntityUseActivated event will
    /// fire if the entity activates a line special or is in range to hit
    /// a blocking line. PlayerUseFail will fire if the entity is a player
    /// and we hit a block line but didn't activate a special.
    /// </summary>
    /// <remarks>
    /// If the line has a special and we are hitting the front then we
    /// can use it (player Z does not apply here). If there's a LineOpening
    /// with OpeningHeight less than or equal to 0, it's a closed sector.
    /// The special line behind it cannot activate until the sector has an
    /// opening.
    /// </remarks>
    /// <param name="entity">The entity to execute use.</param>
    public virtual bool EntityUse(Entity entity)
    {
        if (entity.IsDead)
            return false;

        bool hitBlockLine = false;
        bool activateSuccess = false;
        Vec2D start = entity.Position.XY;
        Vec2D end = start + (Vec2D.UnitCircle(entity.AngleRadians) * entity.Properties.Player.UseRange);
        List<BlockmapIntersect> intersections = BlockmapTraverser.GetBlockmapIntersections(new Seg2D(start, end), BlockmapTraverseFlags.Lines);

        for (int i = 0; i < intersections.Count; i++)
        {
            BlockmapIntersect bi = intersections[i];
            if (bi.Line == null)
                continue;

            if (bi.Line.Segment.OnRight(start))
            {
                if (bi.Line.HasSpecial)
                    activateSuccess = ActivateSpecialLine(entity, bi.Line, ActivationContext.UseLine) || activateSuccess;

                if (activateSuccess && !bi.Line.Flags.PassThrough)
                    break;

                if (bi.Line.Back == null)
                {
                    hitBlockLine = true;
                    break;
                }
            }

            if (bi.Line.Back != null)
            {
                LineOpening opening = PhysicsManager.GetLineOpening(bi.Intersection, bi.Line);
                if (opening.OpeningHeight <= 0)
                {
                    hitBlockLine = true;
                    break;
                }

                // Keep checking if hit two-sided blocking line - this way the PlayerUserFail will be raised if no line special is hit
                if (!opening.CanPassOrStepThrough(entity))
                    hitBlockLine = true;
            }
        }

        DataCache.FreeBlockmapIntersectList(intersections);

        if (!activateSuccess && hitBlockLine && entity.PlayerObj != null)
            entity.PlayerObj.PlayUseFailSound();

        return activateSuccess;
    }

    private void PlayerBumpUse(Entity entity)
    {
        if (Gametick - m_lastBumpActivateGametick < 16)
            return;

        bool shouldUse = false;
        Vec2D start = entity.Position.XY;
        Vec2D end = start + (Vec2D.UnitCircle(entity.AngleRadians) * entity.Properties.Player.UseRange);
        List<BlockmapIntersect> intersections = BlockmapTraverser.GetBlockmapIntersections(new Seg2D(start, end), BlockmapTraverseFlags.Lines);

        for (int i = 0; i < intersections.Count; i++)
        {
            BlockmapIntersect bi = intersections[i];
            if (bi.Line == null)
                continue;

            bool specialActivate = bi.Line.HasSpecial && bi.Line.Segment.OnRight(start);
            if (specialActivate)
                shouldUse = true;

            if (bi.Line.Back == null)
                continue;

            // This is mostly for doors. They can be reversed so ignore it if it's in motion.
            if (specialActivate && SideHasActiveMove(bi.Line.Back.Sector))
            {
                shouldUse = false;
                break;
            }
        }

        if (shouldUse)
        {
            EntityUse(entity);
            m_lastBumpActivateGametick = Gametick;
        }

        DataCache.FreeBlockmapIntersectList(intersections);
    }

    private static bool SideHasActiveMove(Sector sector) => sector.ActiveCeilingMove != null || sector.ActiveFloorMove != null;

    public bool CanActivate(Entity entity, Line line, ActivationContext context)
    {
        bool success = line.Special.CanActivate(entity, line, context,
            ArchiveCollection.Definitions.LockDefininitions, out LockDef? lockFail);
        if (entity.PlayerObj != null && lockFail != null)
        {
            entity.PlayerObj.PlayUseFailSound();
            DisplayMessage(entity.PlayerObj, null, GetLockFailMessage(line, lockFail));
        }
        return success;
    }

    private string GetLockFailMessage(Line line, LockDef lockDef)
    {
        if (line.Special.LineSpecialCompatibility != null &&
            line.Special.LineSpecialCompatibility.CompatibilityType == LineSpecialCompatibilityType.KeyObject)
            return $"{lockDef.Message} to activate this object.";
        else
            return $"{lockDef.Message} to open this door.";
    }

    /// <summary>
    /// Attempts to activate a line special given the entity, line, and context.
    /// </summary>
    /// <remarks>
    /// Does not do any range checking. Only verifies if the entity can activate the line special in this context.
    /// </remarks>
    /// <param name="entity">The entity to execute special.</param>
    /// <param name="line">The line containing the special to execute.</param>
    /// <param name="context">The ActivationContext to attempt to execute the special.</param>
    public virtual bool ActivateSpecialLine(Entity entity, Line line, ActivationContext context)
    {
        if (!CanActivate(entity, line, context))
            return false;

        EntityActivateSpecialEventArgs args = new(context, entity, line);
        EntityActivatedSpecial?.Invoke(this, args);
        return args.Success;
    }

    public bool GetAutoAimEntity(Entity startEntity, in Vec3D start, double angle, double distance, out double pitch, out Entity? entity) =>
        GetAutoAimAngle(startEntity, start, angle, distance, out pitch, out _, out entity, 1, 0);

    public virtual Entity? FireProjectile(Entity shooter, double angle, double pitch, double autoAimDistance, bool autoAim, string projectClassName, out Entity? autoAimEntity,
        double addAngle = 0, double addPitch = 0, double zOffset = 0)
    {
        autoAimEntity = null;
        Player? player = shooter.PlayerObj;
        if (player != null)
            player.DescreaseAmmo();

        Vec3D start = shooter.ProjectileAttackPos;
        start.Z += zOffset;

        if (autoAim && player != null &&
            GetAutoAimAngle(shooter, start, shooter.AngleRadians, autoAimDistance, out double autoAimPitch, out double autoAimAngle,
                out autoAimEntity, tracers: Constants.AutoAimTracers))
        {
            pitch = autoAimPitch;
            angle = autoAimAngle;
        }

        pitch += addPitch;
        angle += addAngle;

        var projectileDef = EntityManager.DefinitionComposer.GetByName(projectClassName);
        if (projectileDef == null)
            return null;

        Entity projectile = EntityManager.Create(projectileDef, start, 0.0, angle, 0, executeStateFunctions: false);

        // Doom set the owner as the target
        projectile.SetOwner(shooter);
        projectile.SetTarget(shooter);
        projectile.PlaySeeSound();

        if (projectile.Flags.Randomize)
            projectile.SetRandomizeTicks();

        if (projectile.Flags.NoClip)
            return projectile;

        Vec3D velocity = Vec3D.UnitSphere(angle, pitch) * projectile.Properties.MissileMovementSpeed;
        Vec3D testPos = projectile.Position;
        if (projectile.Properties.MissileMovementSpeed > 0)
            testPos += Vec3D.UnitSphere(angle, pitch) * (shooter.Radius - 2.0);

        // TryMoveXY will use the velocity of the projectile
        // A projectile spawned where it can't fit can cause BlockingSectorPlane or BlockingEntity (IsBlocked = true)
        if (!projectile.IsBlocked() && PhysicsManager.TryMoveXY(projectile, testPos.XY).Success)
        {
            projectile.SetPosition(testPos);
            projectile.Velocity = velocity;
            return projectile;
        }

        projectile.SetPosition(testPos);
        HandleEntityHit(projectile, velocity, null);
        return null;
    }

    public virtual void FireHitscanBullets(Entity shooter, int bulletCount, double spreadAngleRadians, double spreadPitchRadians, double pitch, double distance, bool autoAim,
        Func<int>? damageFunc = null)
    {
        if (damageFunc == null)
            damageFunc = DefaultDamage;

        if (autoAim)
        {
            Vec3D start = shooter.HitscanAttackPos;
            if (GetAutoAimAngle(shooter, start, shooter.AngleRadians, distance, out double autoAimPitch, out _, out _,
                tracers: Constants.AutoAimTracers))
                pitch = autoAimPitch;
        }

        if (!shooter.Refire && bulletCount == 1)
        {
            int damage = damageFunc();
            FireHitscan(shooter, shooter.AngleRadians, pitch, distance, damage);
        }
        else
        {
            for (int i = 0; i < bulletCount; i++)
            {
                int damage = damageFunc();
                double angle = shooter.AngleRadians + (m_random.NextDiff() * spreadAngleRadians / 255);
                double newPitch = pitch + (m_random.NextDiff() * spreadPitchRadians / 255);
                FireHitscan(shooter, angle, newPitch, distance, damage);
            }
        }
    }

    private int DefaultDamage() => 5 * ((m_random.NextByte() % 3) + 1);

    public virtual Entity? FireHitscan(Entity shooter, double angle, double pitch, double distance, int damage)
    {
        Vec3D start = shooter.HitscanAttackPos;
        Vec3D end = start + Vec3D.UnitSphere(angle, pitch) * distance;
        Vec3D intersect = Vec3D.Zero;

        BlockmapIntersect? bi = FireHitScan(shooter, start, end, pitch, damage <= 0, ref intersect, out Sector? hitSector);

        if (bi != null)
        {
            if (damage > 0)
            {
                // Only move closer on a line hit
                if (bi.Value.Entity == null && hitSector == null)
                    MoveIntersectCloser(start, ref intersect, angle, bi.Value.Distance2D);
                CreateBloodOrPulletPuff(bi.Value.Entity, intersect, angle, distance, damage);
            }

            if (bi.Value.Entity != null)
            {
                DamageEntity(bi.Value.Entity, shooter, damage, DamageType.AlwaysApply, Thrust.Horizontal);
                return bi.Value.Entity;
            }
        }

        return null;
    }

    public virtual BlockmapIntersect? FireHitScan(Entity shooter, Vec3D start, Vec3D end, double pitch, bool isTest,
        ref Vec3D intersect, out Sector? hitSector)
    {
        hitSector = null;
        BlockmapIntersect? returnValue = null;
        double floorZ, ceilingZ;
        Seg2D seg = new(start.XY, end.XY);
        List<BlockmapIntersect> intersections = BlockmapTraverser.ShootTraverse(seg);

        for (int i = 0; i < intersections.Count; i++)
        {
            BlockmapIntersect bi = intersections[i];
            if (bi.Line != null)
            {
                if (!isTest && bi.Line.HasSpecial && CanActivate(shooter, bi.Line, ActivationContext.HitscanImpactsWall))
                {
                    var args = new EntityActivateSpecialEventArgs(ActivationContext.HitscanImpactsWall, shooter, bi.Line);
                    EntityActivatedSpecial?.Invoke(this, args);
                }

                intersect = bi.Intersection.To3D(start.Z + (Math.Tan(pitch) * bi.Distance2D));

                if (bi.Line.Back == null)
                {
                    floorZ = bi.Line.Front.Sector.ToFloorZ(intersect);
                    ceilingZ = bi.Line.Front.Sector.ToCeilingZ(intersect);

                    if (intersect.Z > floorZ && intersect.Z < ceilingZ)
                    {
                        returnValue = bi;
                        break;
                    }

                    if (IsSkyClipOneSided(bi.Line.Front.Sector, floorZ, ceilingZ, intersect))
                        break;

                    GetSectorPlaneIntersection(start, end, bi.Line.Front.Sector, floorZ, ceilingZ, ref intersect);
                    hitSector = bi.Line.Front.Sector;
                    DataCache.FreeBlockmapIntersectList(intersections);
                    return bi;
                }

                GetOrderedSectors(bi.Line, start, out Sector front, out Sector back);
                if (IsSkyClipTwoSided(front, back, intersect))
                    break;

                floorZ = front.ToFloorZ(intersect);
                ceilingZ = front.ToCeilingZ(intersect);

                if (intersect.Z < floorZ || intersect.Z > ceilingZ)
                {
                    GetSectorPlaneIntersection(start, end, front, floorZ, ceilingZ, ref intersect);
                    hitSector = front;
                    returnValue = bi;
                    break;
                }

                LineOpening opening = PhysicsManager.GetLineOpening(bi.Intersection, bi.Line);
                if ((opening.FloorZ > intersect.Z && intersect.Z > floorZ) || (opening.CeilingZ < intersect.Z && intersect.Z < ceilingZ))
                {
                    returnValue = bi;
                    break;
                }
            }
            else if (bi.Entity != null && !ReferenceEquals(shooter, bi.Entity) && bi.Entity.Box.Intersects(start, end, ref intersect))
            {
                returnValue = bi;
                break;
            }
        }

        DataCache.FreeBlockmapIntersectList(intersections);
        return returnValue;
    }

    public virtual bool DamageEntity(Entity target, Entity? source, int damage, DamageType damageType,
        Thrust thrust = Thrust.HorizontalAndVertical, Sector? sectorSource = null)
    {
        if (!target.Flags.Shootable || damage == 0)
            return false;

        Vec3D thrustVelocity = Vec3D.Zero;
        if (source != null && thrust != Thrust.None)
        {
            Vec3D savePos = source.Position;
            // Check if the souce is owned by this target and the same position and move to get a valid thrust angle. (player shot missile against wall)
            if (source.Owner.Entity == target && source.Position.XY == target.Position.XY)
            {
                Vec3D move = (source.Position.XY + Vec2D.UnitCircle(target.AngleRadians) * 2).To3D(source.Position.Z);
                source.SetPosition(move);
            }

            Vec2D xyDiff = source.Position.XY - target.Position.XY;
            bool zEqual = Math.Abs(target.Position.Z - source.Position.Z) <= double.Epsilon;
            bool xyEqual = Math.Abs(xyDiff.X) <= 1.0 && Math.Abs(xyDiff.Y) <= 1.0;
            double pitch = 0.0;

            double angle = source.Position.Angle(target.Position);
            double thrustAmount = damage * source.ProjectileKickBack * 0.125 / target.Properties.Mass;

            // Silly vanilla doom feature that allows target to be thrown forward sometimes
            if (damage < 40 && damage > target.Health &&
                target.Position.Z - source.Position.Z > 64 && (m_random.NextByte() & 1) != 0)
            {
                angle += Math.PI;
                thrustAmount *= 4;
            }

            if (thrust == Thrust.HorizontalAndVertical)
            {
                // Player rocket jumping check, back up the source Z to get a valid pitch
                // Only done for players, otherwise blowing up enemies will launch them in the air
                if (zEqual && target.IsPlayer && source.Owner.Entity == target)
                {
                    Vec3D sourcePos = new Vec3D(source.Position.X, source.Position.Y, source.Position.Z - 1.0);
                    pitch = sourcePos.Pitch(target.Position, 0.0);
                }
                else if (source.Position.Z < target.Position.Z || source.Position.Z > target.Position.Z + target.Height)
                {
                    Vec3D sourcePos = source.CenterPoint;
                    Vec3D targetPos = target.Position;
                    if (source.Position.Z > target.Position.Z + target.Height)
                        targetPos.Z += target.Height;
                    pitch = sourcePos.Pitch(targetPos, sourcePos.XY.Distance(targetPos.XY));
                }

                if (!xyEqual)
                    thrustVelocity = Vec3D.UnitSphere(angle, 0.0);

                thrustVelocity.Z = Math.Sin(pitch);
            }
            else
            {
                thrustVelocity = Vec3D.UnitSphere(angle, 0.0);
            }

            thrustVelocity *= thrustAmount;
            if (savePos != source.Position)
                source.SetPosition(savePos);
        }

        bool setPainState = m_random.NextByte() < target.Properties.PainChance;
        if (target.PlayerObj != null)
        {
            // Voodoo dolls did not take sector damage in the original
            if (target.PlayerObj.IsVooDooDoll && sectorSource != null)
                return false;
            // Sector damage is applied to real players, but not their voodoo dolls
            if (sectorSource == null)
                ApplyVooDooDamage(target.PlayerObj, damage, setPainState);
        }

        if (target.Damage(source, damage, setPainState, damageType) || target.IsInvulnerable)
            target.Velocity += thrustVelocity;

        return true;
    }

    public virtual bool GiveItem(Player player, Entity item, EntityFlags? flags, out EntityDefinition definition, bool pickupFlash = true)
    {
        definition = item.Definition;
        GiveVooDooItem(player, item, flags, pickupFlash);

        if (ArchiveCollection.Definitions.DehackedDefinition != null && GetDehackedPickup(ArchiveCollection.Definitions.DehackedDefinition, item, out var vanillaDef))
        {
            definition = vanillaDef;
            flags = vanillaDef.Flags;
            return player.GiveItem(vanillaDef, flags, pickupFlash);
        }

        return player.GiveItem(item.Definition, flags, pickupFlash);
    }

    private bool GetDehackedPickup(DehackedDefinition dehacked, Entity item, [NotNullWhen(true)] out EntityDefinition? definition)
    {
        // Vanilla determined pickups by the sprite name
        // E.g. batman doom has an enemy that drops a shotgun with the blue key sprite
        if (!dehacked.PickupLookup.TryGetValue(item.Frame.Sprite, out string? def))
        {
            definition = null;
            return false;
        }

        definition = ArchiveCollection.EntityDefinitionComposer.GetByName(def);
        return definition!= null;
    }

    public virtual void PerformItemPickup(Entity entity, Entity item)
    {
        if (entity.PlayerObj == null)
            return;

        int health = entity.PlayerObj.Health;
        if (!GiveItem(entity.PlayerObj, item, item.Flags, out EntityDefinition definition))
            return;

        item.PickupPlayer = entity.PlayerObj;
        item.FrameState.SetState("Pickup", warn: false);

        if (item.Flags.CountItem)
        {
            LevelStats.ItemCount++;
            entity.PlayerObj.ItemCount++;
        }

        string message = definition.Properties.Inventory.PickupMessage;
        var healthProperty = definition.Properties.HealthProperty;
        if (healthProperty != null && health < healthProperty.Value.LowMessageHealth && healthProperty.Value.LowMessage.Length > 0)
            message = healthProperty.Value.LowMessage;

        DisplayMessage(entity.PlayerObj, null, message);
        EntityManager.Destroy(item);

        if (!string.IsNullOrEmpty(definition.Properties.Inventory.PickupSound))
        {
            SoundManager.CreateSoundOn(entity, definition.Properties.Inventory.PickupSound,
                new SoundParams(entity, channel: SoundChannel.Item));
        }
    }

    public virtual void HandleEntityHit(Entity entity, in Vec3D previousVelocity, TryMoveData? tryMove)
    {
        if (entity.IsDisposed)
            return;

        entity.Hit(previousVelocity);

        if (tryMove != null && (entity.Flags.Missile || entity.IsPlayer))
        {
            for (int i = 0; i < tryMove.ImpactSpecialLines.Count; i++)
                ActivateSpecialLine(entity, tryMove.ImpactSpecialLines[i], ActivationContext.EntityImpactsWall);

            if (entity.IsPlayer && Config.Game.BumpUse)
                PlayerBumpUse(entity);
        }

        if (entity.ShouldDieOnCollison())
        {
            if (entity.BlockingEntity != null)
            {
                int damage = entity.Properties.Damage.Get(m_random);
                DamageEntity(entity.BlockingEntity, entity, damage, DamageType.Normal);
            }

            bool skyClip = false;

            if (entity.BlockingLine != null)
            {
                if (entity.BlockingLine.OneSided && IsSkyClipOneSided(entity.BlockingLine.Front.Sector, entity.BlockingLine.Front.Sector.ToFloorZ(entity.Position),
                    entity.BlockingLine.Front.Sector.ToCeilingZ(entity.Position), entity.Position))
                {
                    skyClip = true;
                }
                else if (!entity.BlockingLine.OneSided)
                {
                    GetOrderedSectors(entity.BlockingLine, entity.Position, out Sector front, out Sector back);
                    if (IsSkyClipTwoSided(front, back, entity.Position))
                        skyClip = true;
                }
            }

            if (entity.BlockingSectorPlane != null && ArchiveCollection.TextureManager.IsSkyTexture(entity.BlockingSectorPlane.TextureHandle))
                skyClip = true;

            if (skyClip)
                EntityManager.Destroy(entity);
            else
                entity.SetDeathState(null);
        }
        else if (entity.Flags.Touchy || (entity.BlockingEntity != null && entity.BlockingEntity.Flags.Touchy))
        {
            if (entity.BlockingEntity != null && ShouldDieFromTouch(entity, entity.BlockingEntity))
                entity.BlockingEntity.Kill(null);
            else if (entity.IsCrushing())
                entity.Kill(null);
        }
    }

    public virtual void HandleEntityIntersections(Entity entity, in Vec3D previousVelocity, TryMoveData? tryMove)
    {
        if (tryMove == null || tryMove.IntersectEntities2D.Count == 0)
            return;

        for (int i = 0; i < tryMove.IntersectEntities2D.Count; i++)
        {
            Entity intersectEntity = tryMove.IntersectEntities2D[i];
            if (!entity.Box.OverlapsZ(intersectEntity.Box) || ReferenceEquals(entity, intersectEntity))
                continue;

            if (entity.Flags.Ripper && !ReferenceEquals(entity.Owner.Entity, intersectEntity))
                RipDamage(entity, intersectEntity);
            if (intersectEntity.Flags.Touchy && ShouldDieFromTouch(entity, intersectEntity))
                intersectEntity.Kill(null);
        }
    }

    private void RipDamage(Entity source, Entity target)
    {
        int damage = source.Definition.Properties.Damage.Get(m_random);
        if (DamageEntity(target, source, damage, DamageType.Normal, Thrust.None))
        {
            CreateBloodOrPulletPuff(target, source.Position, source.AngleRadians, 0, damage, true);
            string sound = "misc/ripslop";
            if (source.Properties.RipSound.Length > 0)
                sound = source.Properties.RipSound;
            SoundManager.CreateSoundOn(source, sound, new SoundParams(source));
        }
    }

    private static bool ShouldDieFromTouch(Entity entity, Entity blockingEntity)
    {
        // The documentation on Touchy is horrible
        // Based on testing crushers will kill it and it will only be killed if something walks into it
        // But not the other way around...
        // LostSouls will not kill PainElementals
        const string painElemental = "PainElemental";
        const string lostSoul = "LostSoul";
        if (!blockingEntity.Flags.Touchy || !blockingEntity.CanDamage(entity, DamageType.Normal))
            return false;

        if (entity.Definition.IsType(painElemental) && blockingEntity.Definition.IsType(lostSoul))
            return false;

        if (entity.Definition.IsType(lostSoul) && blockingEntity.Definition.IsType(painElemental))
            return false;

        return true;
    }

    public virtual bool CheckLineOfSight(Entity from, Entity to)
    {
        Vec2D start = from.Position.XY;
        Vec2D end = to.Position.XY;

        if (start == end)
            return true;

        Seg2D seg = new(start, end);
        List<BlockmapIntersect> intersections = BlockmapTraverser.SightTraverse(seg, out bool hitOneSidedLine);
        if (hitOneSidedLine)
        {
            DataCache.FreeBlockmapIntersectList(intersections);
            return false;
        }

        Vec3D sightPos = new(from.Position.X, from.Position.Y, from.Position.Z + (from.Height * 0.75));
        double distance2D = start.Distance(end);
        double topPitch = sightPos.Pitch(to.Position.Z + to.Height, distance2D);
        double bottomPitch = sightPos.Pitch(to.Position.Z, distance2D);

        TraversalPitchStatus status = GetBlockmapTraversalPitch(intersections, sightPos, from, topPitch, bottomPitch, out _, out _);
        DataCache.FreeBlockmapIntersectList(intersections);
        return status != TraversalPitchStatus.Blocked;
    }

    public virtual bool InFieldOfView(Entity from, Entity to, double fieldOfViewRadians)
    {
        Vec2D entityLookingVector = Vec2D.UnitCircle(from.AngleRadians);
        Vec2D entityToTarget = to.Position.XY - from.Position.XY;
        entityToTarget.Normalize();
        var angle = Math.Acos(entityToTarget.Dot(entityLookingVector));
        return angle < fieldOfViewRadians/2;
    }

    private static bool InFieldOfViewOrInMeleeDistance(Entity from, Entity to)
    {
        double distance = from.Position.ApproximateDistance2D(to.Position);
        Vec2D entityLookingVector = Vec2D.UnitCircle(from.AngleRadians);
        Vec2D entityToTarget = to.Position.XY - from.Position.XY;

        // Not in front 180 FOV
        if (entityToTarget.Dot(entityLookingVector) < 0 && distance > Constants.EntityMeleeDistance)
            return false;

        return true;
    }

    public virtual void RadiusExplosion(Entity damageSource, Entity attackSource, int radius, int maxDamage)
    {
        Thrust thrust = damageSource.Flags.OldRadiusDmg ? Thrust.Horizontal : Thrust.HorizontalAndVertical;
        Vec2D pos2D = damageSource.Position.XY;
        Vec2D radius2D = new(radius, radius);
        Box2D explosionBox = new(pos2D - radius2D, pos2D + radius2D);

        List<BlockmapIntersect> intersections = BlockmapTraverser.GetBlockmapIntersections(explosionBox, BlockmapTraverseFlags.Entities,
            BlockmapTraverseEntityFlags.Shootable | BlockmapTraverseEntityFlags.Solid);
        for (int i = 0; i < intersections.Count; i++)
        {
            BlockmapIntersect bi = intersections[i];
            if (bi.Entity != null && ShouldApplyExplosionDamage(bi.Entity, damageSource))
                ApplyExplosionDamageAndThrust(damageSource, attackSource, bi.Entity, radius, maxDamage, thrust,
                    damageSource.Flags.OldRadiusDmg || bi.Entity.Flags.OldRadiusDmg);
        }

        DataCache.FreeBlockmapIntersectList(intersections);
    }

    private bool ShouldApplyExplosionDamage(Entity entity, Entity damageSource)
    {
        if (entity.Flags.NoRadiusDmg && !damageSource.Flags.ForceRadiusDmg)
            return false;

        if (!entity.CanApplyRadiusExplosionDamage(damageSource) || !CheckLineOfSight(entity, damageSource))
            return false;

        return true;
    }

    public virtual TryMoveData TryMoveXY(Entity entity, Vec2D position)
        => PhysicsManager.TryMoveXY(entity, position);

    public virtual bool IsPositionValid(Entity entity, Vec2D position) =>
        PhysicsManager.IsPositionValid(entity, position);

    public virtual SectorMoveStatus MoveSectorZ(double speed, double destZ, SectorMoveSpecial moveSpecial)
         => PhysicsManager.MoveSectorZ(speed, destZ, moveSpecial);

    public virtual void HandleEntityDeath(Entity deathEntity, Entity? deathSource, bool gibbed)
    {
        PhysicsManager.HandleEntityDeath(deathEntity);
        CheckDropItem(deathEntity);

        if (deathEntity.Flags.CountKill && !deathEntity.Flags.Friendly)
            LevelStats.KillCount++;

        if (deathEntity.PlayerObj != null)
        {
            if (deathSource != null)
                HandleObituary(deathEntity.PlayerObj, deathSource);

            ApplyVooDooKill(deathEntity.PlayerObj, deathSource, gibbed);
        }
    }

    private void CheckDropItem(Entity deathEntity)
    {
        if (deathEntity.Definition.Properties.DropItem != null &&
            (deathEntity.Definition.Properties.DropItem.Probability == DropItemProperty.DefaultProbability ||
                m_random.NextByte() < deathEntity.Definition.Properties.DropItem.Probability))
        {
            for (int i = 0; i < deathEntity.Definition.Properties.DropItem.Amount; i++)
            {
                Vec3D pos = deathEntity.Position;
                pos.Z += deathEntity.Definition.Properties.Height / 2;
                Entity? dropItem = EntityManager.Create(deathEntity.Definition.Properties.DropItem.ClassName, pos);
                if (dropItem != null)
                {
                    dropItem.Flags.Dropped = true;
                    dropItem.Velocity.Z += 4;
                }
            }
        }
    }

    private void HandleObituary(Player player, Entity deathSource)
    {
        if (ArchiveCollection.IWadType == IWadBaseType.ChexQuest)
            return;

        // If the player killed themself then don't display the obituary message
        // There is probably a special string for this in multiplayer for later
        Entity killer = deathSource.Owner.Entity ?? deathSource;
        if (ReferenceEquals(player, killer))
            return;

        // Monster obituaries can come from the projectile, while the player obituaries always come from the owner player
        Entity obituarySource = killer;
        if (killer.IsPlayer)
            obituarySource = deathSource;

        string? obituary;
        if (obituarySource == deathSource && obituarySource.Definition.Properties.HitObituary.Length > 0)
            obituary = obituarySource.Definition.Properties.HitObituary;
        else
            obituary = obituarySource.Definition.Properties.Obituary;

        if (!string.IsNullOrEmpty(obituary))
            DisplayMessage(player, killer.PlayerObj, obituary);
    }

    public virtual void DisplayMessage(Player player, Player? other, string message)
    {
        message = ArchiveCollection.Definitions.Language.GetMessage(player, other, message);
        if (message.Length > 0)
            Log.Info(message);
    }

    private void HandleRespawn(Entity entity)
    {
        entity.Respawn = false;
        if (entity.Definition.Flags.Solid && IsPositionBlockedByEntity(entity, entity.SpawnPoint))
            return;

        Entity? newEntity = EntityManager.Create(entity.Definition, entity.SpawnPoint, 0, entity.AngleRadians, entity.ThingId, true);
        if (newEntity != null)
        {
            CreateTeleportFog(entity.Position);
            CreateTeleportFog(entity.SpawnPoint);

            newEntity.Flags.Friendly = entity.Flags.Friendly;
            newEntity.AngleRadians = entity.AngleRadians;
            newEntity.ReactionTime = 18;

            entity.Dispose();
        }
    }

    public bool IsPositionBlockedByEntity(Entity entity, in Vec3D position)
    {
        if (!entity.Definition.Flags.Solid)
            return true;

        double oldHeight = entity.Height;
        entity.Flags.Solid = true;
        entity.SetHeight(entity.Definition.Properties.Height);

        // This is original functionality, the original game only checked against other things
        // It didn't check if it would clip into map geometry
        bool blocked = entity.GetIntersectingEntities3D(position, BlockmapTraverseEntityFlags.Solid).Count > 0;
        entity.Flags.Solid = false;
        entity.SetHeight(oldHeight);

        return blocked;
    }

    private readonly TryMoveData EmtpyTryMove = new();

    public bool IsPositionBlocked(Entity entity)
    {
        if (entity.GetIntersectingEntities3D(entity.Position, BlockmapTraverseEntityFlags.Solid).Count > 0)
            return true;

        if (!PhysicsManager.IsPositionValid(entity, entity.Position.XY, EmtpyTryMove))
            return true;

        return false;
    }

    private void ApplyExplosionDamageAndThrust(Entity source, Entity attackSource, Entity entity, double radius, int maxDamage, Thrust thrust,
        bool approxDistance2D)
    {
        double distance;

        if (thrust == Thrust.HorizontalAndVertical && (source.Position.Z < entity.Position.Z || source.Position.Z >= entity.Box.Top))
        {
            Vec3D sourcePos = source.Position;
            Vec3D targetPos = entity.Position;

            if (source.Position.Z > entity.Position.Z)
                targetPos.Z += entity.Height;

            if (approxDistance2D)
                distance = Math.Max(0.0, sourcePos.ApproximateDistance2D(targetPos) - entity.Radius);
            else
                distance = Math.Max(0.0, sourcePos.Distance(targetPos) - entity.Radius);
        }
        else
        {
            if (approxDistance2D)
                distance = entity.Position.ApproximateDistance2D(source.Position) - entity.Radius;
            else
                distance = entity.Position.Distance(source.Position) - entity.Radius;
        }

        int applyDamage = Math.Clamp((int)(radius - distance), 0, maxDamage);
        if (applyDamage <= 0)
            return;

        Entity? originalOwner = source.Owner.Entity;
        source.SetOwner(attackSource);
        DamageEntity(entity, source, applyDamage, DamageType.AlwaysApply, thrust);
        source.SetOwner(originalOwner);
    }

    protected void ChangeToLevel(int number)
    {
        LevelExit?.Invoke(this, new LevelChangeEvent(number));
    }

    protected bool ChangeToMusic(int number)
    {
        if (this is SinglePlayerWorld singlePlayerWorld)
        {
            if (!MapWarp.GetMap(number, ArchiveCollection.Definitions.MapInfoDefinition.MapInfo, out MapInfoDef? mapInfoDef) || mapInfoDef == null)
                return false;

            SinglePlayerWorld.PlayLevelMusic(singlePlayerWorld.AudioSystem, mapInfoDef.Music, ArchiveCollection);
            return true;
        }

        return false;
    }

    protected void ResetLevel(bool loadLastWorldModel)
    {
        LevelChangeType type = loadLastWorldModel ? LevelChangeType.ResetOrLoadLast : LevelChangeType.Reset;
        LevelExit?.Invoke(this, new LevelChangeEvent(type));
    }

    protected virtual void PerformDispose()
    {
        SpecialManager.Dispose();
        EntityManager.Dispose();
        SoundManager.Dispose();
    }

    private void CreateBloodOrPulletPuff(Entity? entity, Vec3D intersect, double angle, double attackDistance, int damage, bool ripper = false)
    {
        bool bulletPuff = entity == null || entity.Definition.Flags.NoBlood;
        string className;
        if (bulletPuff)
        {
            className = "BulletPuff";
            intersect.Z += Random.NextDiff() * Constants.PuffRandZ;
        }
        else
        {
            className = entity!.GetBloodType();
        }

        Entity? create = EntityManager.Create(className, intersect);
        if (create == null)
            return;

        create.AngleRadians = angle;
        if (bulletPuff)
        {
            create.Velocity.Z = 1;
            if (create.Flags.Randomize)
                create.SetRandomizeTicks();

            // Doom would skip the initial sparking state of the bullet puff for punches
            // Bulletpuff decorate has a MELEESTATE for this
            if (attackDistance == Constants.EntityMeleeDistance)
                create.SetMeleeState();
        }
        else
        {
            SetBloodValues(entity, create, damage, ripper);
        }
    }

    private void SetBloodValues(Entity? entity, Entity blood, int damage, bool ripper)
    {
        if (ripper)
        {
            if (entity != null)
            {
                blood.Velocity.X = entity.Velocity.X / 2;
                blood.Velocity.Y = entity.Velocity.Y / 2;
            }

            blood.Velocity.X += m_random.NextDiff() / 16.0;
            blood.Velocity.Y += m_random.NextDiff() / 16.0;
            blood.Velocity.Z += m_random.NextDiff() / 16.0;
            return;
        }

        blood.Velocity.Z = 2;

        int offset = 0;
        if (damage <= 12 && damage >= 9)
            offset = 1;
        else if (damage < 9)
            offset = 2;

        if (offset == 0)
            blood.SetRandomizeTicks();
        else
            blood.FrameState.SetState(Constants.FrameStates.Spawn, offset);
    }

    private static void MoveIntersectCloser(in Vec3D start, ref Vec3D intersect, double angle, double distXY)
    {
        distXY -= 2.0;
        intersect.X = start.X + (Math.Cos(angle) * distXY);
        intersect.Y = start.Y + (Math.Sin(angle) * distXY);
    }

    /// <summary>
    /// Fires when an entity activates a line special with use or by crossing a line.
    /// </summary>
    /// <param name="shooter">The entity firing.</param>
    /// <param name="start">The position the enity is firing from.</param>
    /// <param name="angle">The angle the entity is firing.</param>
    /// <param name="distance">The distance to use for firing.</param>
    /// <param name="pitch">The pitch to use for the hit entity.</param>
    /// <param name="setAngle">The angle to use for the hit entity.</param>
    /// <param name="entity">The hit entity.</param>
    /// <param name="tracers">The number of tracers to use excluding the angle of the player. Vanilla doom used 2.</param>
    /// <returns>True if a valid entity is found and the pitch is set.</returns>
    /// <param name="tracerSpread">Doom would check at -5 degress and +5 degrees for a hit as well.
    /// Doom used the pitch for hitscan weapons, but would use the angle as well for projectiles.</param>
    private bool GetAutoAimAngle(Entity shooter, in Vec3D start, double angle, double distance,
        out double pitch, out double setAngle, out Entity? entity,
        int tracers = 0, double tracerSpread = Constants.DefaultSpreadAngle)
    {
        entity = null;
        pitch = 0;
        setAngle = angle;

        double spread;
        int iterateTracers;
        if (tracers <= 1)
        {
            spread = 0;
            tracers = 1;
            iterateTracers = 1;
        }
        else
        {
            spread = tracerSpread / (tracers / 2);
            iterateTracers = tracers + 1;
        }

        for (int i = 0; i < iterateTracers; i++)
        {
            Seg2D seg = new(start.XY, (start + Vec3D.UnitSphere(setAngle, 0) * distance).XY);
            List<BlockmapIntersect> intersections = BlockmapTraverser.ShootTraverse(seg);

            TraversalPitchStatus status = GetBlockmapTraversalPitch(intersections, start, shooter, MaxPitch, MinPitch, out pitch, out entity);
            DataCache.FreeBlockmapIntersectList(intersections);

            if (status == TraversalPitchStatus.PitchSet)
                return true;

            setAngle += spread;
            if (i == tracers / 2)
                setAngle = angle - tracerSpread;
        }

        return false;
    }

    private enum TraversalPitchStatus
    {
        Blocked,
        PitchSet,
        PitchNotSet,
    }

    private TraversalPitchStatus GetBlockmapTraversalPitch(List<BlockmapIntersect> intersections, in Vec3D start, Entity startEntity, double topPitch, double bottomPitch,
        out double pitch, out Entity? entity)
    {
        pitch = 0.0;
        entity = null;

        for (int i = 0; i < intersections.Count; i++)
        {
            BlockmapIntersect bi = intersections[i];

            if (bi.Line != null)
            {
                if (bi.Line.Back == null)
                    return TraversalPitchStatus.Blocked;

                LineOpening opening = PhysicsManager.GetLineOpening(bi.Intersection, bi.Line);
                if (opening.FloorZ < opening.CeilingZ)
                {
                    double sectorPitch = start.Pitch(opening.FloorZ, bi.Distance2D);
                    if (sectorPitch > bottomPitch)
                        bottomPitch = sectorPitch;

                    sectorPitch = start.Pitch(opening.CeilingZ, bi.Distance2D);
                    if (sectorPitch < topPitch)
                        topPitch = sectorPitch;

                    if (topPitch <= bottomPitch)
                        return TraversalPitchStatus.Blocked;
                }
                else
                {
                    return TraversalPitchStatus.Blocked;
                }
            }
            else if (bi.Entity != null && !ReferenceEquals(startEntity, bi.Entity))
            {
                double thingTopPitch = start.Pitch(bi.Entity.Box.Max.Z, bi.Distance2D);
                if (thingTopPitch < bottomPitch)
                    continue;

                double thingBottomPitch = start.Pitch(bi.Entity.Box.Min.Z, bi.Distance2D);
                if (thingBottomPitch > topPitch)
                    continue;

                if (thingBottomPitch > topPitch)
                    return TraversalPitchStatus.Blocked;
                if (thingTopPitch < bottomPitch)
                    return TraversalPitchStatus.Blocked;

                if (thingTopPitch < topPitch)
                    topPitch = thingTopPitch;
                if (thingBottomPitch > bottomPitch)
                    bottomPitch = thingBottomPitch;

                pitch = (bottomPitch + topPitch) / 2.0;
                entity = bi.Entity;
                return TraversalPitchStatus.PitchSet;
            }
        }

        return TraversalPitchStatus.PitchNotSet;
    }

    private bool IsSkyClipOneSided(Sector sector, double floorZ, double ceilingZ, in Vec3D intersect)
    {
        if (intersect.Z > ceilingZ && ArchiveCollection.TextureManager.IsSkyTexture(sector.Ceiling.TextureHandle))
            return true;
        else if (intersect.Z < floorZ && ArchiveCollection.TextureManager.IsSkyTexture(sector.Floor.TextureHandle))
            return true;

        return false;
    }

    private bool IsSkyClipTwoSided(Sector front, Sector back, in Vec3D intersect)
    {
        bool isFrontCeilingSky = ArchiveCollection.TextureManager.IsSkyTexture(front.Ceiling.TextureHandle);
        bool isBackCeilingSky = ArchiveCollection.TextureManager.IsSkyTexture(back.Ceiling.TextureHandle);

        if (isFrontCeilingSky && isBackCeilingSky && intersect.Z > back.ToCeilingZ(intersect))
            return true;

        if (isFrontCeilingSky && intersect.Z > front.ToCeilingZ(intersect))
            return true;

        if (ArchiveCollection.TextureManager.IsSkyTexture(front.Floor.TextureHandle) && intersect.Z < front.ToFloorZ(intersect))
            return true;

        return false;
    }

    private static void GetSectorPlaneIntersection(in Vec3D start, in Vec3D end, Sector sector, double floorZ, double ceilingZ, ref Vec3D intersect)
    {
        if (intersect.Z < floorZ)
        {
            sector.Floor.Plane.Intersects(start, end, ref intersect);
            intersect.Z = sector.ToFloorZ(intersect);
        }
        else if (intersect.Z > ceilingZ)
        {
            sector.Ceiling.Plane.Intersects(start, end, ref intersect);
            intersect.Z = sector.ToCeilingZ(intersect) - 4;
        }
    }

    private static void GetOrderedSectors(Line line, in Vec3D start, out Sector front, out Sector back)
    {
        if (line.Segment.OnRight(start))
        {
            front = line.Front.Sector;
            back = line.Back!.Sector;
        }
        else
        {
            front = line.Back!.Sector;
            back = line.Front.Sector;
        }
    }

    public void CreateTeleportFog(in Vec3D pos, bool playSound = true)
    {
        EntityDefinition? teleportFog = EntityManager.DefinitionComposer.GetByName("TeleportFog");
        if (teleportFog != null)
        {
            var teleport = EntityManager.Create(teleportFog, pos, 0.0, 0.0, 0);
            if (teleport != null)
            {
                teleport.SetZ(teleport.Sector.ToFloorZ(pos), false);
                SoundManager.CreateSoundOn(teleport, Constants.TeleportSound, new SoundParams(teleport));
            }
        }
    }

    public void ActivateCheat(Player player, ICheat cheat)
    {
        if (!string.IsNullOrEmpty(cheat.CheatOn))
        {
            string msg;
            if (cheat.IsToggleCheat)
                msg = player.Cheats.IsCheatActive(cheat.CheatType) ? cheat.CheatOn : cheat.CheatOff;
            else
                msg = cheat.CheatOn;
            DisplayMessage(player, null, msg);
        }

        if (cheat is LevelCheat levelCheat)
        {
            if (levelCheat.CheatType == CheatType.ChangeLevel)
            {
                ChangeToLevel(levelCheat.LevelNumber);
                return;
            }
            else if (levelCheat.CheatType == CheatType.ChangeMusic && !ChangeToMusic(levelCheat.LevelNumber))
            {
                return;
            }
        }

        switch (cheat.CheatType)
        {
            case CheatType.NoClip:
                player.Flags.NoClip = player.Cheats.IsCheatActive(cheat.CheatType);
                break;
            case CheatType.Fly:
                player.Flags.NoGravity = player.Cheats.IsCheatActive(cheat.CheatType);
                break;
            case CheatType.Kill:
                player.ForceGib();
                break;
            case CheatType.Ressurect:
                if (player.IsDead)
                    player.SetRaiseState();
                break;
            case CheatType.God:
                if (!player.IsDead)
                    SetGodModeHealth(player);
                player.Flags.Invulnerable = player.Cheats.IsCheatActive(cheat.CheatType);
                break;
            case CheatType.GiveAllNoKeys:
                GiveAllWeapons(player);
                GiveCheatArmor(player, cheat.CheatType);
                break;
            case CheatType.GiveAll:
                GiveAllWeapons(player);
                player.Inventory.GiveAllKeys(EntityManager.DefinitionComposer);
                GiveCheatArmor(player, cheat.CheatType);
                break;
            case CheatType.Chainsaw:
                GiveChainsaw(player);
                break;
            case CheatType.BeholdRadSuit:
            case CheatType.BeholdPartialInvisibility:
            case CheatType.BeholdInvulnerability:
            case CheatType.BeholdComputerAreaMap:
            case CheatType.BeholdLightAmp:
            case CheatType.BeholdBerserk:
            case CheatType.Automap:
                TogglePowerup(player, PowerupNameFromCheatType(cheat.CheatType), PowerupTypeFromCheatType(cheat.CheatType));
                break;
            default:
                break;
        }
    }

    private void SetGodModeHealth(Player player)
    {
        if (ArchiveCollection.Dehacked != null && ArchiveCollection.Dehacked.Misc != null && ArchiveCollection.Dehacked.Misc.GodModeHealth.HasValue)
        {
            if (ArchiveCollection.Dehacked.Misc.GodModeHealth.Value > 0)
                player.Health = ArchiveCollection.Dehacked.Misc.GodModeHealth.Value;
        }
        else
        {
            player.Health = player.Definition.Properties.Player.MaxHealth;
        }
    }

    public int EntityAliveCount(int editorId)
    {
        int count = 0;
        LinkableNode<Entity>? node = Entities.Head;
        while (node != null)
        {
            Entity entity = node.Value;
            if (entity.Definition.EditorId.HasValue && entity.Definition.EditorId.Value == editorId && !entity.IsDead)
                count++;
            node = node.Next;
        }
        return count;
    }

    public bool HealChase(Entity entity, EntityFrame healState, string healSound)
    {
        Box2D nextBox = new(entity.GetNextEnemyPos(), entity.Radius);
        List<BlockmapIntersect> intersections = entity.World.BlockmapTraverser.GetBlockmapIntersections(nextBox,
            BlockmapTraverseFlags.Entities, BlockmapTraverseEntityFlags.Corpse);

        for (int i = 0; i < intersections.Count; i++)
        {
            BlockmapIntersect bi = intersections[i];

            if (bi.Entity == null || !bi.Entity.HasRaiseState() || bi.Entity.FrameState.Frame.Ticks != -1 || bi.Entity.IsPlayer)
                continue;

            if (bi.Entity.World.IsPositionBlockedByEntity(bi.Entity, bi.Entity.Position))
                continue;

            bi.Entity.Flags.Solid = true;
            bi.Entity.SetHeight(entity.Definition.Properties.Height);

            Entity? saveTarget = entity.Target.Entity;
            entity.SetTarget(bi.Entity);
            EntityActionFunctions.A_FaceTarget(entity);
            entity.SetTarget(saveTarget);
            entity.FrameState.SetState(healState);

            if (healSound.Length > 0)
                entity.SoundManager.CreateSoundOn(bi.Entity, healSound, new SoundParams(entity));

            bi.Entity.SetRaiseState();
            bi.Entity.Flags.Friendly = entity.Flags.Friendly;

            DataCache.FreeBlockmapIntersectList(intersections);
            return true;
        }

        DataCache.FreeBlockmapIntersectList(intersections);
        return false;
    }

    public void TracerSeek(Entity entity, double threshold, double maxTurnAngle, GetTracerVelocityZ velocityZ)
    {
        if (entity.Tracer.Entity == null || entity.Tracer.Entity.IsDead)
            return;

        SetTracerAngle(entity, threshold, maxTurnAngle);

        double z = entity.Velocity.Z;
        entity.Velocity = Vec3D.UnitSphere(entity.AngleRadians, 0.0) * entity.Definition.Properties.MissileMovementSpeed;
        entity.Velocity.Z = z;

        entity.Velocity.Z = velocityZ(entity, entity.Tracer.Entity);
    }

    public void SetNewTracerTarget(Entity entity, double fieldOfViewRadians, double radius)
    {
        Entity owner = entity.Owner.Entity ?? entity;
        List<BlockmapIntersect> intersections = BlockmapTraverser.GetBlockmapIntersections(new Box2D(entity.Position.XY, radius), 
            BlockmapTraverseFlags.Entities, BlockmapTraverseEntityFlags.Shootable);

        for (int i= 0; i < intersections.Count; i++)
        {
            Entity? checkEntity = intersections[i].Entity;
            if (checkEntity == null || !owner.ValidEnemyTarget(checkEntity))
                continue;

            if (fieldOfViewRadians > 0 && !InFieldOfView(entity, checkEntity, fieldOfViewRadians))
                continue;

            if (!CheckLineOfSight(entity, checkEntity))
                continue;

            entity.SetTracer(checkEntity);
            break;
        }

        DataCache.FreeBlockmapIntersectList(intersections);
    }

    private static void SetTracerAngle(Entity entity, double threshold, double maxTurnAngle)
    {
        if (entity.Tracer.Entity == null)
            return;
        // Doom's angles were always 0-360 and did not allow negatives (thank you arithmetic overflow)
        // To keep this code familiar GetPositiveAngle will keep angle between 0 and 2pi
        double exact = MathHelper.GetPositiveAngle(entity.Position.Angle(entity.Tracer.Entity.Position));
        double currentAngle = MathHelper.GetPositiveAngle(entity.AngleRadians);
        double diff = MathHelper.GetPositiveAngle(exact - currentAngle);

        if (!MathHelper.AreEqual(exact, currentAngle))
        {
            if (diff > Math.PI)
            {
                entity.AngleRadians = MathHelper.GetPositiveAngle(entity.AngleRadians - maxTurnAngle);
                if (MathHelper.GetPositiveAngle(exact - entity.AngleRadians) < threshold)
                    entity.AngleRadians = exact;
            }
            else
            {
                entity.AngleRadians = MathHelper.GetPositiveAngle(entity.AngleRadians + maxTurnAngle);
                if (MathHelper.GetPositiveAngle(exact - entity.AngleRadians) > threshold)
                    entity.AngleRadians = exact;
            }
        }
    }

    private static double GetTracerSlope(double z, double distance, double speed)
    {
        distance /= speed;
        if (distance < 1)
            distance = 1;
        return z / distance;
    }

    private void GiveCheatArmor(Player player, CheatType cheatType)
    {
        bool autoGive = true;
        int? setAmount = null;
        if (ArchiveCollection.Dehacked != null && ArchiveCollection.Dehacked.Misc != null)
        {
            var misc = ArchiveCollection.Dehacked.Misc;
            if ((cheatType == CheatType.GiveAll && misc.IdkfaArmorClass == DehackedDefinition.GreenArmorClassNum) ||
                (cheatType == CheatType.GiveAllNoKeys && misc.IdfaArmorClass == DehackedDefinition.GreenArmorClassNum))
            {
                var armorDef = EntityManager.DefinitionComposer.GetByName(DehackedDefinition.GreenArmorClassName);
                if (armorDef != null)
                    player.GiveItem(armorDef, null, false);
                autoGive = false;
            }

            if (cheatType == CheatType.GiveAll)
                setAmount = misc.IdkfaArmor;
            else if (cheatType == CheatType.GiveAllNoKeys)
                setAmount = misc.IdfaArmor;
        }

        if (autoGive)
        {
            var armor = EntityManager.DefinitionComposer.GetEntityDefinitions().Where(x => x.IsType(Inventory.ArmorClassName) && x.EditorId.HasValue)
                .OrderByDescending(x => x.Properties.Armor.SaveAmount).ToList();

            if (armor.Any())
                player.GiveItem(armor.First(), null, pickupFlash: false);
        }

        if (setAmount.HasValue)
            player.Armor = setAmount.Value;
    }

    private void TogglePowerup(Player player, string powerupDefinition, PowerupType powerupType)
    {
        if (string.IsNullOrEmpty(powerupDefinition) || powerupType == PowerupType.None)
            return;

        var def = EntityManager.DefinitionComposer.GetByName(powerupDefinition);
        if (def == null)
            return;

        // Not really a powerup, part of inventory
        if (powerupType == PowerupType.ComputerAreaMap)
        {
            if (player.Inventory.HasItem(def.Name))
                player.Inventory.Remove(def.Name, 1);
            else
                player.Inventory.Add(def, 1);
        }
        else
        {
            var existingPowerup = player.Inventory.Powerups.FirstOrDefault(x => x.PowerupType == powerupType);
            if (existingPowerup != null)
                player.Inventory.RemovePowerup(existingPowerup);
            else
                player.Inventory.Add(def, 1);
        }
    }

    private static string PowerupNameFromCheatType(CheatType cheatType)
    {
        switch (cheatType)
        {
            case CheatType.Automap:
                return "Allmap";
            case CheatType.BeholdRadSuit:
                return "RadSuit";
            case CheatType.BeholdPartialInvisibility:
                return "BlurSphere";
            case CheatType.BeholdInvulnerability:
                return "InvulnerabilitySphere";
            case CheatType.BeholdComputerAreaMap:
                return "Allmap";
            case CheatType.BeholdLightAmp:
                return "Infrared";
            case CheatType.BeholdBerserk:
                return "Berserk";
            default:
                break;
        }

        return string.Empty;
    }

    private static PowerupType PowerupTypeFromCheatType(CheatType cheatType)
    {
        switch (cheatType)
        {
            case CheatType.BeholdRadSuit:
                return PowerupType.IronFeet;
            case CheatType.BeholdPartialInvisibility:
                return PowerupType.Invisibility;
            case CheatType.BeholdInvulnerability:
                return PowerupType.Invulnerable;
            case CheatType.BeholdComputerAreaMap:
                return PowerupType.ComputerAreaMap;
            case CheatType.BeholdLightAmp:
                return PowerupType.LightAmp;
            case CheatType.BeholdBerserk:
                return PowerupType.Strength;
            case CheatType.Automap:
                return PowerupType.ComputerAreaMap;
            default:
                break;
        }

        return PowerupType.None;
    }

    private void GiveChainsaw(Player player)
    {
        var chainsaw = EntityManager.DefinitionComposer.GetByName("chainsaw");
        if (chainsaw != null)
            player.GiveWeapon(chainsaw);
    }

    private void GiveAllWeapons(Player player)
    {
        foreach (string name in player.Inventory.Weapons.GetWeaponDefinitionNames())
        {
            var weapon = EntityManager.DefinitionComposer.GetByName(name);
            if (weapon != null)
                player.GiveWeapon(weapon, autoSwitch: false);
        }

        player.Inventory.GiveAllAmmo(EntityManager.DefinitionComposer);
    }

    private void ApplyVooDooDamage(Player player, int damage, bool setPainState)
    {
        if (EntityManager.VoodooDolls.Count == 0 || player.IsSyncVooDoo)
            return;

        SyncVooDooDollsWithPlayer(player.PlayerNumber);

        foreach (var updatePlayer in EntityManager.Players.Union(EntityManager.VoodooDolls))
        {
            if (updatePlayer == player || updatePlayer.PlayerNumber != player.PlayerNumber)
                continue;

            updatePlayer.Damage(null, damage, setPainState, DamageType.AlwaysApply);
        }

        CompleteVooDooDollSync();
    }

    private void ApplyVooDooKill(Player player, Entity? source, bool forceGib)
    {
        if (EntityManager.VoodooDolls.Count == 0 || player.IsSyncVooDoo)
            return;

        SyncVooDooDollsWithPlayer(player.PlayerNumber);

        foreach (var updatePlayer in EntityManager.Players.Union(EntityManager.VoodooDolls))
        {
            if (updatePlayer == player || updatePlayer.PlayerNumber != player.PlayerNumber || updatePlayer.IsDead)
                continue;

            if (forceGib)
                updatePlayer.ForceGib();
            else
                updatePlayer.Kill(source);
        }

        CompleteVooDooDollSync();
    }

    private void GiveVooDooItem(Player player, Entity item, EntityFlags? flags, bool pickupFlash)
    {
        if (EntityManager.VoodooDolls.Count == 0)
            return;

        SyncVooDooDollsWithPlayer(player.PlayerNumber);

        foreach (var updatePlayer in EntityManager.Players.Union(EntityManager.VoodooDolls))
        {
            if (updatePlayer == player || updatePlayer.PlayerNumber != player.PlayerNumber)
                continue;

            bool success = updatePlayer.GiveItem(item.Definition, flags, pickupFlash);
            if (success && !updatePlayer.IsVooDooDoll && !string.IsNullOrEmpty(item.Definition.Properties.Inventory.PickupSound))
            {
                SoundManager.CreateSoundOn(updatePlayer, item.Definition.Properties.Inventory.PickupSound, 
                    new SoundParams(updatePlayer, channel: SoundChannel.Item));
            }
        }

        CompleteVooDooDollSync();
    }

    private void SyncVooDooDollsWithPlayer(int playerNumber)
    {
        Player? realPlayer = GetRealPlayer(playerNumber);
        if (realPlayer == null)
            return;

        foreach (var player in EntityManager.Players)
            player.IsSyncVooDoo = true;

        foreach (var voodooDoll in EntityManager.VoodooDolls)
        {
            voodooDoll.IsSyncVooDoo = true;
            voodooDoll.VodooSync(realPlayer);
        }
    }

    private void CompleteVooDooDollSync()
    {
        foreach (var player in EntityManager.Players)
            player.IsSyncVooDoo = false;

        foreach (var voodooDoll in EntityManager.VoodooDolls)
            voodooDoll.IsSyncVooDoo = false;
    }

    private Player? GetRealPlayer(int playerNumber)
        => EntityManager.Players.FirstOrDefault(x => x.PlayerNumber == playerNumber && !x.IsVooDooDoll);

    public WorldModel ToWorldModel()
    {
        List<SectorModel> sectorModels = new();
        List<SectorDamageSpecialModel> sectorDamageSpecialModels = new();
        SetSectorModels(sectorModels, sectorDamageSpecialModels);

        return new WorldModel()
        {
            ConfigValues = GetConfigValuesModel(),
            Files = GetGameFilesModel(),
            MapName = MapName.ToString(),
            WorldState = WorldState,
            Gametick = Gametick,
            LevelTime = LevelTime,
            SoundCount = m_soundCount,
            Gravity = Gravity,
            RandomIndex = Random.RandomIndex,
            Skill = ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkillLevel(SkillDefinition),
            CurrentBossTarget = CurrentBossTarget,

            Players = GetPlayerModels(),
            Entities = GetEntityModels(),
            Sectors = sectorModels,
            DamageSpecials = sectorDamageSpecialModels,
            Lines = GetLineModels(),
            Specials = SpecialManager.GetSpecialModels(),
            VisitedMaps = GlobalData.VisitedMaps.Select(x => x.MapName).ToList(),
            TotalTime = GlobalData.TotalTime,

            TotalMonsters = LevelStats.TotalMonsters,
            TotalItems = LevelStats.TotalItems,
            TotalSecrets = LevelStats.TotalSecrets,
            KillCount = LevelStats.KillCount,
            ItemCount = LevelStats.ItemCount,
            SecretCount = LevelStats.SecretCount
        };
    }

    private IList<ConfigValueModel> GetConfigValuesModel()
    {
        List<ConfigValueModel> items = new();
        foreach (var (path, component) in Config.GetComponents())
        {
            if (!component.Attribute.Serialize)
                continue;

            items.Add(new ConfigValueModel(path, component.Value.ObjectValue));
        }
        return items;
    }

    public GameFilesModel GetGameFilesModel()
    {
        return new GameFilesModel()
        {
            IWad = GetIWadFileModel(),
            Files = GetFileModels(),
        };
    }

    private IList<PlayerModel> GetPlayerModels()
    {
        List<PlayerModel> playerModels = new(EntityManager.Players.Count);
        EntityManager.Players.ForEach(player => playerModels.Add(player.ToPlayerModel()));
        EntityManager.VoodooDolls.ForEach(player => playerModels.Add(player.ToPlayerModel()));
        return playerModels;
    }

    private FileModel GetIWadFileModel()
    {
        Archive? archive = ArchiveCollection.IWad;
        if (archive != null)
            return archive.ToFileModel();

        return new FileModel();
    }

    private IList<FileModel> GetFileModels()
    {
        List<FileModel> fileModels = new();
        var archives = ArchiveCollection.Archives;
        foreach (var archive in archives)
        {
            if (archive.ExtractedFrom != null || archive.MD5 == Archive.DefaultMD5)
                continue;
            fileModels.Add(archive.ToFileModel());
        }

        return fileModels;
    }

    private List<EntityModel> GetEntityModels()
    {
        List<EntityModel> entityModels = new();
        LinkableNode<Entity>? node = EntityManager.Entities.Head;
        while (node != null)
        {
            Entity entity = node.Value;
            if (!entity.IsPlayer)
                entityModels.Add(entity.ToEntityModel(new EntityModel()));
            node = node.Next;
        }
        return entityModels;
    }

    private void SetSectorModels(List<SectorModel> sectorModels, List<SectorDamageSpecialModel> sectorDamageSpecialModels)
    {
        for (int i = 0; i < Sectors.Count; i++)
        {
            Sector sector = Sectors[i];
            if (sector.SoundTarget.Entity != null || sector.DataChanged)
                sectorModels.Add(sector.ToSectorModel());
            if (sector.SectorDamageSpecial != null)
                sectorDamageSpecialModels.Add(sector.SectorDamageSpecial.ToSectorDamageSpecialModel());
        }
    }

    private List<LineModel> GetLineModels()
    {
        List<LineModel> lineModels = new();
        for (int i = 0; i < Lines.Count; i++)
        {
            Line line = Lines[i];
            if (!line.DataChanged)
                continue;

            lineModels.Add(line.ToLineModel());
        }

        return lineModels;
    }
}
