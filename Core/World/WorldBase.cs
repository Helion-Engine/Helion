using Helion.Audio;
using Helion.Dehacked;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Graphics.Palettes;
using Helion.Maps;
using Helion.Maps.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.Compatibility;
using Helion.Maps.Specials.Vanilla;
using Helion.Maps.Specials.ZDoom;
using Helion.Models;
using Helion.Render.OpenGL.Renderers.Legacy.World.Primitives;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Compatibility;
using Helion.Resources.Definitions.Locks;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.Definitions.MusInfo;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.Util.Loggers;
using Helion.Util.Profiling;
using Helion.Util.RandomGenerators;
using Helion.Util.Timing;
using Helion.World.Blockmap;
using Helion.World.Bsp;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Definition.Properties.Components;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Geometry.Islands;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Helion.World.Physics.Blockmap;
using Helion.World.Sound;
using Helion.World.Special;
using Helion.World.Special.Specials;
using Helion.World.Static;
using Helion.World.Stats;
using Helion.World.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using static Helion.Dehacked.DehackedDefinition;
using static Helion.Util.Assertion.Assert;

namespace Helion.World;

public abstract partial class WorldBase : IWorld
{
    const int BspBlockDimension = 16;
    public const int DefaultLineOfSightDistance = 1024;
    private const double MaxPitch = 80.0 * Math.PI / 180.0;
    private const double MinPitch = -80.0 * Math.PI / 180.0;

    private static BlockMap? LastBlockMap;
    private static BlockMap? LastRenderBlockMap;
    private static uint[]? LastBspBlockmapNodeIndices;
    private static GridDimensions LastBspBlockmapDimensions;

    private static WorldSoundManager? LastWorldSoundManager;
    private static EntityManager? LastEntityManager;
    private static PhysicsManager? LastPhysicManager;
    private static SpecialManager? LastSpecialManager;
    private static DynamicArray<StructLine> LastStructLines = new();

    public event EventHandler<LevelChangeEvent>? LevelExit;
    public event EventHandler? LevelExiting;
    public event EventHandler? WorldPaused;
    public event EventHandler? WorldResumed;
    public event EventHandler? ClearConsole;
    public event EventHandler? OnResetInterpolation;
    public event EventHandler<SectorPlane>? SectorMoveStart;
    public event EventHandler<SectorPlane>? SectorMoveComplete;
    public event EventHandler<SectorPlane>? SectorMove;
    public event EventHandler<SideTextureEvent>? SideTextureChanged;
    public event EventHandler<PlaneTextureEvent>? PlaneTextureChanged;
    public event EventHandler<Sector>? SectorLightChanged;
    public event EventHandler<Sector>? SectorColorMapChanged;
    public event EventHandler<PlayerMessageEvent>? PlayerMessage;
    public event EventHandler<MusicChangeEvent>? OnMusicChanged;
    public event EventHandler? OnTick;
    public event EventHandler? OnDestroying;

    private static int StaticId;
    public abstract WorldType WorldType { get; }
    public int Id { get; } = StaticId++;

    public readonly long CreationTimeNanos;
    public string MapName { get; protected set; }
    public BlockMap Blockmap { get; private set; }
    public WorldState WorldState { get; protected set; } = WorldState.Normal;
    public int Gametick { get; private set; }
    public int GameTicker { get; private set; }
    public int LevelTime { get; private set; }
    public double Gravity { get; private set; } = 1.0;
    public bool Paused { get; protected set; }
    public bool DrawPause { get; protected set; }
    public bool PlayingDemo { get; set; }
    public bool DemoEnded { get; set; }
    public bool SameAsPreviousMap { get; set; }
    public IRandom Random => m_random;
    public IRandom SecondaryRandom { get; private set; }
    public List<Line> Lines { get; private set; }
    public List<Side> Sides { get; private set; }
    public List<Sector> Sectors { get; private set; }
    public DynamicArray<StructLine> StructLines => LastStructLines;
    public List<HighlightArea> HighlightAreas { get; } = [];
    public CompactBspTree BspTree { get; private set; }
    public EntityManager EntityManager { get; }
    public WorldSoundManager SoundManager { get; }
    public BlockmapTraverser BlockmapTraverser => PhysicsManager.BlockmapTraverser;
    public BlockMap RenderBlockmap { get; private set; }
    public SpecialManager SpecialManager { get; private set; }
    public IConfig Config { get; private set; }
    public MapInfoDef MapInfo { get; set; }
    public LevelStats LevelStats { get; } = new();
    public SkillDef SkillDefinition { get; private set; }
    public SkillLevel SkillLevel { get; private set; }
    public ArchiveCollection ArchiveCollection { get; protected set; }
    public GlobalData GlobalData { get; }
    public CheatManager CheatManager { get; } = new();
    public DataCache DataCache => ArchiveCollection.DataCache;
    public abstract Player Player { get; protected set; }
    public List<IMonsterCounterSpecial> BossDeathSpecials => m_bossDeathSpecials;
    public bool IsFastMonsters { get; private set; }
    public virtual bool IsChaseCamMode => false;
    public bool DrawHud { get; protected set; } = true;
    public bool AnyLayerObscuring { get; set; }
    public bool IsDisposed { get; private set; }
    public abstract ListenerParams GetListener();
    public int CurrentBossTarget { get; set; }
    public MarkSpecials MarkSpecials { get; } = new();

    public GameInfoDef GameInfo => ArchiveCollection.Definitions.MapInfoDefinition.GameDefinition;
    public TextureManager TextureManager => ArchiveCollection.TextureManager;

    public MapGeometry Geometry { get; }
    public PhysicsManager PhysicsManager { get; private set; }
    public CompatibilityMapDefinition? CompatibilityMapDefinition { get; private set; }
    public MapType MapType { get; private set; }

    public bool HasDehacked;

    protected readonly IAudioSystem AudioSystem;

    protected readonly Profiler Profiler;
    private readonly IRandom m_saveRandom;
    private IRandom m_random;
    private int m_exitTicks;
    private int m_easyBossBrain;
    private int m_soundCount;
    private int m_lastBumpActivateGametick;
    private ExitLevelArgs m_exitLevelArgs;
    private Entity[] m_bossBrainTargets = [];
    private readonly List<IMonsterCounterSpecial> m_bossDeathSpecials = [];
    private readonly byte[] m_lineOfSightReject = [];
    private readonly Func<DamageFuncParams, int> m_defaultDamageAction;
    private readonly EntityDefinition? m_teleportFogDef;
    private readonly Dictionary<int, MusInfoDef> m_sectorToMusicChange = [];
    private readonly DynamicArray<Entity> m_fallCheckEntities = new(32);
    private readonly Dictionary<int, Player> m_itemPickupIndexToPlayers = [];
    private readonly Entity m_checkRadiusEntity;
    private readonly Dictionary<int, LineHealthGroup> m_lineHealthGroups = [];
    private readonly IMap m_map;
    private readonly SpawnMulti m_spawnMulti;
    private readonly DynamicArray<SlopeSpan> m_visibleSpans = new(16);
    private MusInfoDef? m_lastMusicChange;
    private int m_changeMusicTicks;
    private int m_losDistance = DefaultLineOfSightDistance;
    private string m_activeMusic = string.Empty;
    private bool m_explosionTraverseLines;
    private Sector? m_lastSector3D;
    private int m_pitchOnBlockLine;

    const int HighlightSize = 112;
    private readonly List<object> m_findObjects = [];

    private RadiusExplosionData m_radiusExplosion;
    private readonly Action<Entity> m_radiusExplosionEntityAction;
    private readonly Action<int> m_radiusExplosionLineAction;

    private HealChaseData m_healChaseData;
    private readonly Action<Entity> m_healChaseAction;

    private NewTracerTargetData m_newTracerTargetData;
    private readonly Func<Entity, GridIterationStatus> m_setNewTracerTargetAction;

    private LineOfSightEnemyData m_lineOfSightEnemyData;
    private readonly Func<Entity, GridIterationStatus> m_lineOfSightEnemyAction;

    protected WorldBase(GlobalData globalData, IConfig config, ArchiveCollection archiveCollection,
        IAudioSystem audioSystem, Profiler profiler, MapGeometry geometry, MapInfoDef mapInfoDef,
        SkillDef skillDef, IMap map, WorldModel? worldModel = null, IRandom? random = null, bool sameAsPreviousMap = false, bool reuse = true)
    {
        Lines = geometry.Lines;
        Sides = geometry.Sides;
        Sectors = geometry.Sectors;
        SameAsPreviousMap = sameAsPreviousMap;
        m_random = random ?? new DoomRandom();
        m_saveRandom = m_random;
        SecondaryRandom = m_random.Clone();
        m_map = map;

        CreationTimeNanos = Ticker.NanoTime();
        GlobalData = globalData;
        ArchiveCollection = archiveCollection;
        AudioSystem = audioSystem;
        Config = config;
        MapInfo = mapInfoDef;
        SkillDefinition = skillDef;
        SkillLevel = ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkillLevel(skillDef);
        MapName = map.Name;
        Profiler = profiler;
        Geometry = geometry;
        CompatibilityMapDefinition = map.CompatibilityDefinition;
        MapType = map.MapType;
        BspTree = Geometry.CompactBspTree;

        if (map.Reject != null && map.Reject.Length > 0)
        {
            int rejectSize = (Sectors.Count * Sectors.Count + 7) / 8;
            if (map.Reject.Length != rejectSize)
                HelionLog.Warn($"Expected reject size to be {rejectSize} but read {map.Reject.Length} bytes");
            m_lineOfSightReject = map.Reject;
        }

        Blockmap = CreateBlockMap();
        RenderBlockmap = CreateRenderBlockMap();

        SoundManager = CreateSoundManager();
        EntityManager = CreateEntityManager(reuse);
        PhysicsManager = CreatePhysicsManager();
        SpecialManager = CreateSpecialManager(reuse);

        IsFastMonsters = skillDef.IsFastMonsters(config);
        m_spawnMulti = skillDef.SpawnMulti;
        if (m_spawnMulti == SpawnMulti.SinglePlayerAndCoop && CompatibilityMapDefinition != null && CompatibilityMapDefinition.Parent.SetSpawnMultiToCoopOnly)
            m_spawnMulti = SpawnMulti.CoopOnly;

        m_defaultDamageAction = DefaultDamage;
        m_radiusExplosionEntityAction = HandleRadiusExplosionEntity;
        m_radiusExplosionLineAction = HandleRadiusExplosionLine;
        m_healChaseAction = HandleHealChase;
        m_setNewTracerTargetAction = HandleSetNewTracerTarget;
        m_lineOfSightEnemyAction = HandleLineOfSightEnemy;

        m_teleportFogDef = EntityManager.DefinitionComposer.GetByName("TeleportFog");

        HasDehacked = ArchiveCollection.Definitions.DehackedDefinition != null;

        SetWorldStatic();
        BuildLines(); // MidTex3D lines creating entities makes it dependent on WorldStatic
        RegisterConfigChanges();

        m_checkRadiusEntity = new Entity();
        m_checkRadiusEntity.Set(0, 0, 0, new EntityDefinition(0, "CHECK_RADIUS", null, []), default, 0, Sector.CreateDefault(), this, default);

        if (worldModel != null)
        {
            WorldState = worldModel.WorldState;
            Gametick = worldModel.Gametick;
            LevelTime = worldModel.LevelTime;
            m_soundCount = worldModel.SoundCount;
            Gravity = worldModel.Gravity;
            Random.Clone(worldModel.RandomIndex);
            CurrentBossTarget = worldModel.CurrentBossTarget;
            GlobalData.VisitedMaps = GetVisitedMaps(worldModel.VisitedMaps);
            GlobalData.TotalTime = worldModel.TotalTime;
            LevelStats.TotalMonsters = worldModel.TotalMonsters;
            LevelStats.TotalItems = worldModel.TotalItems;
            LevelStats.TotalSecrets = worldModel.TotalSecrets;
            LevelStats.KillCount = worldModel.KillCount;
            LevelStats.ItemCount = worldModel.ItemCount;
            LevelStats.SecretCount = worldModel.SecretCount;
        }

        if (!SameAsPreviousMap)
            SpecialManager.InitSectors3D();
    }

    private SpecialManager CreateSpecialManager(bool reuse)
    {
        if (reuse && LastSpecialManager != null)
        {
            LastSpecialManager.UpdateTo(this, m_random);
            return LastSpecialManager;
        }

        LastSpecialManager = new(this, m_random);
        return LastSpecialManager;
    }

    private PhysicsManager CreatePhysicsManager()
    {
        if (LastPhysicManager != null)
        {
            LastPhysicManager.UpdateTo(this, BspTree, Blockmap, Random, MapType == MapType.Doom);
            return LastPhysicManager;
        }

        LastPhysicManager = new(this, BspTree, Blockmap, Random, MapType == MapType.Doom);
        return LastPhysicManager;
    }

    private EntityManager CreateEntityManager(bool reuse)
    {
        if (reuse && LastEntityManager != null)
        {
            LastEntityManager.UpdateTo(this);
            return LastEntityManager;
        }
        LastEntityManager = new(this);
        return LastEntityManager;
    }

    private WorldSoundManager CreateSoundManager()
    {
        if (LastWorldSoundManager != null)
        {
            LastWorldSoundManager.UpdateTo(this);
            return LastWorldSoundManager;
        }

        LastWorldSoundManager = new WorldSoundManager(this, AudioSystem);
        return LastWorldSoundManager;
    }

    private BlockMap CreateBlockMap()
    {
        if (SameAsPreviousMap && LastBlockMap != null)
        {
            m_bspBlockmapDimensions = LastBspBlockmapDimensions;
            m_bspBlockmapNodeIndices = LastBspBlockmapNodeIndices!;
            LastBlockMap.Clear();
            return LastBlockMap;
        }

        LastBlockMap = new BlockMap(Lines, 128);
        CreateBspBlockMap(LastBlockMap);

        return LastBlockMap;
    }

    private BlockMap CreateRenderBlockMap()
    {
        if (SameAsPreviousMap && LastRenderBlockMap != null)
        {
            LastRenderBlockMap.Clear();
            return LastRenderBlockMap;
        }

        LastRenderBlockMap = new BlockMap(Blockmap.Bounds, 512);
        return LastRenderBlockMap;
    }

    private void BuildLines()
    {
        if (SameAsPreviousMap)
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                var line = Lines[i];
                ref StructLine structLine = ref StructLines.Data[i];
                structLine.Flags &= ~StructLineFlags.SeenForAutomap;
                structLine.Update(line);
                m_explosionTraverseLines = m_explosionTraverseLines || line.ObjectHealth != ObjectHealth.Default;
            }
            return;
        }

        LastStructLines.Clear();
        LastStructLines.EnsureCapacityExact(Lines.Count);
        LastStructLines.SetLength(Lines.Count);
        var arrayData = LastStructLines.Data;
        var lineCounts = new LineCounts[Sectors.Count];

        for (int i = 0; i < Lines.Count; i++)
        {
            var line = Lines[i];
            arrayData[i] = new StructLine(line);
            var objectHealth = line.ObjectHealth != ObjectHealth.Default;
            m_explosionTraverseLines = m_explosionTraverseLines || objectHealth;

            if (objectHealth && line.ObjectHealth.HealthGroup != 0)
            {
                if (!m_lineHealthGroups.TryGetValue(line.ObjectHealth.HealthGroup, out var group))
                {
                    group = new();
                    m_lineHealthGroups[line.ObjectHealth.HealthGroup] = group;
                }

                group.Lines.Add(line);
            }

            var midtex = line.Flags.Blocking.MidTex3D;
            ref var counts = ref lineCounts[line.Front.Sector.Id];
            counts.LineCount++;
            if (midtex)
                counts.MidTexCount++;

            if (line.Back != null)
            {
                counts = ref lineCounts[line.Back.Sector.Id];
                counts.LineCount++;
                if (midtex)
                    counts.MidTexCount++;
            }

            // Allocate entity ahead of time
            if (midtex)
                line.GetMidTexEntity(this);
        }

        for (int i = 0; i < Sectors.Count; i++)
        {
            var sector = Sectors[i];
            var counts = lineCounts[i];
            lineCounts[i] = default;

            if (counts.LineCount == 0)
                continue;

            sector.Lines = new Line[counts.LineCount];
            sector.LineIds = new int[counts.LineCount];

            if (counts.MidTexCount > 0)
                sector.MidTex3DLines = new Line[counts.MidTexCount];
        }

        for (int i = 0; i < Lines.Count; i++)
        {
            var line = Lines[i];
            var frontSector = line.Front.Sector;
            var midtex = line.Flags.Blocking.MidTex3D;
            ref var counts = ref lineCounts[frontSector.Id];
            frontSector.Lines[counts.LineCount] = line;
            frontSector.LineIds[counts.LineCount++] = i;

            if (midtex)
                frontSector.MidTex3DLines[counts.MidTexCount++] = line;

            if (line.Back != null)
            {
                var backSector = line.Back.Sector;
                counts = ref lineCounts[backSector.Id];
                backSector.Lines[counts.LineCount] = line;
                backSector.LineIds[counts.LineCount++] = i;

                if (midtex)
                    backSector.MidTex3DLines[counts.MidTexCount++] = line;
            }
        }
    }

    private void SetupMusicChangers()
    {
        foreach (var entity in EntityManager.MusicChangers)
        {
            if (!GetMusInfoFromEntity(entity, out var musInfo))
                continue;

            m_sectorToMusicChange[entity.Sector.Id] = musInfo;

            // Cache the entry to prevent stutters
            Entry? entry = ArchiveCollection.Entries.FindByName(musInfo.Name);
            if (entry != null)
                musInfo.MusicData = entry.ReadData();
        }
    }

    private bool GetMusInfoFromEntity(Entity entity, [NotNullWhen(true)] out MusInfoDef? musInfo)
    {
        musInfo = null;
        int musicNumber = entity.ThingId;
        if (musicNumber < 0)
            return false;

        // Number 0 is the map's default music.
        if (musicNumber == 0)
        {
            musInfo = new MusInfoDef(0, MapInfo.Music);
            return true;
        }

        var map = ArchiveCollection.Definitions.MusInfoDefinition.Items.FirstOrDefault(x => x.MapName.EqualsIgnoreCase(MapInfo.MapName));
        if (map == null)
            return false;

        musInfo = map.Music.FirstOrDefault(x => x.Number == musicNumber);
        return musInfo != null;
    }

    private void RegisterConfigChanges()
    {
        Config.SlowTick.Enabled.OnChanged += SlowTickEnabled_OnChanged;
        Config.SlowTick.ChaseFailureSkipCount.OnChanged += SlowTickChaseFailureSkipCount_OnChanged;
        Config.SlowTick.Distance.OnChanged += SlowTickDistance_OnChanged;
        Config.SlowTick.ChaseMultiplier.OnChanged += SlowTickChaseMultiplier_OnChanged;
        Config.SlowTick.LookMultiplier.OnChanged += SlowTickLookMultiplier_OnChanged;
        Config.SlowTick.TracerMultiplier.OnChanged += SlowTickTracerMultiplier_OnChanged;

        Config.Compatibility.MissileClip.OnChanged += MissileClip_OnChanged;
        Config.Compatibility.AllowItemDropoff.OnChanged += AllowItemDropoff_OnChanged;
        Config.Compatibility.InfinitelyTallThings.OnChanged += InfinitelyTallThings_OnChanged;
        Config.Compatibility.NoTossDrops.OnChanged += NoTossDrops_OnChanged;
        Config.Compatibility.VanillaMovementPhysics.OnChanged += VanillaMovementPhysics_OnChanged;
        Config.Compatibility.Mbf21.OnChanged += Mbf21_OnChanged;
        Config.Compatibility.Doom2ProjectileWalkTriggers.OnChanged += Doom2ProjectileWalkTriggers_OnChanged;
        Config.Compatibility.OriginalExplosion.OnChanged += OriginalExplosion_OnChanged;
        Config.Compatibility.FinalDoomTeleport.OnChanged += FinalDoomTeleport_OnChanged;
        Config.Compatibility.VanillaSectorSound.OnChanged += VanillaSectorSound_OnChanged;

        Config.Game.FastMonsters.OnChanged += FastMonsters_OnChanged;
        Config.Game.DamageApplyMultiplier.OnChanged += DamageApplyMultiplier_OnChanged;
        Config.Game.DamageReceiveMultiplier.OnChanged += DamageReceiveMultiplier_OnChanged;
        Config.Game.MirrorCorpse.OnChanged += MirrorCorpse_OnChanged;
    }

    private void UnRegisterConfigChanges()
    {
        Config.SlowTick.Enabled.OnChanged -= SlowTickEnabled_OnChanged;
        Config.SlowTick.ChaseFailureSkipCount.OnChanged -= SlowTickChaseFailureSkipCount_OnChanged;
        Config.SlowTick.Distance.OnChanged -= SlowTickDistance_OnChanged;
        Config.SlowTick.ChaseMultiplier.OnChanged -= SlowTickChaseMultiplier_OnChanged;
        Config.SlowTick.LookMultiplier.OnChanged -= SlowTickLookMultiplier_OnChanged;
        Config.SlowTick.TracerMultiplier.OnChanged -= SlowTickTracerMultiplier_OnChanged;

        Config.Compatibility.MissileClip.OnChanged -= MissileClip_OnChanged;
        Config.Compatibility.AllowItemDropoff.OnChanged -= AllowItemDropoff_OnChanged;
        Config.Compatibility.InfinitelyTallThings.OnChanged -= InfinitelyTallThings_OnChanged;
        Config.Compatibility.NoTossDrops.OnChanged -= NoTossDrops_OnChanged;
        Config.Compatibility.VanillaMovementPhysics.OnChanged -= VanillaMovementPhysics_OnChanged;
        Config.Compatibility.Mbf21.OnChanged -= Mbf21_OnChanged;
        Config.Compatibility.Doom2ProjectileWalkTriggers.OnChanged -= Doom2ProjectileWalkTriggers_OnChanged;
        Config.Compatibility.OriginalExplosion.OnChanged -= OriginalExplosion_OnChanged;
        Config.Compatibility.FinalDoomTeleport.OnChanged -= FinalDoomTeleport_OnChanged;
        Config.Compatibility.VanillaSectorSound.OnChanged -= VanillaSectorSound_OnChanged;

        Config.Game.FastMonsters.OnChanged -= FastMonsters_OnChanged;
        Config.Game.DamageApplyMultiplier.OnChanged -= DamageApplyMultiplier_OnChanged;
        Config.Game.DamageReceiveMultiplier.OnChanged -= DamageReceiveMultiplier_OnChanged;
        Config.Game.MirrorCorpse.OnChanged -= MirrorCorpse_OnChanged;
    }

    private void SetWorldStatic()
    {
        Entity.ClosetChaseCount = 0;
        Entity.ClosetLookCount = 0;
        Entity.ChaseLoop = 0;
        Entity.ChaseFailureCount = 0;

        WorldStatic.World = this;
        WorldStatic.DataCache = DataCache;
        WorldStatic.EntityManager = EntityManager;
        WorldStatic.SoundManager = SoundManager;
        WorldStatic.EntityManager = EntityManager;
        WorldStatic.Frames = ArchiveCollection.Definitions.EntityFrameTable.Frames;
        WorldStatic.Random = Random;
        WorldStatic.SlowTickEnabled = Config.SlowTick.Enabled.Value;
        WorldStatic.SlowTickChaseFailureSkipCount = (short)Config.SlowTick.ChaseFailureSkipCount;
        WorldStatic.SlowTickDistance = (short)Config.SlowTick.Distance;
        WorldStatic.SlowTickChaseMultiplier = (short)Config.SlowTick.ChaseMultiplier;
        WorldStatic.SlowTickLookMultiplier = (short)Config.SlowTick.LookMultiplier;
        WorldStatic.SlowTickTracerMultiplier = (short)Config.SlowTick.TracerMultiplier;
        WorldStatic.IsFastMonsters = IsFastMonsters;
        WorldStatic.IsSlowMonsters = SkillDefinition.SlowMonsters;
        WorldStatic.InfinitelyTallThings = Config.Compatibility.InfinitelyTallThings;
        WorldStatic.MissileClip = Config.Compatibility.MissileClip;
        WorldStatic.AllowItemDropoff = Config.Compatibility.AllowItemDropoff;
        WorldStatic.NoTossDrops = Config.Compatibility.NoTossDrops;
        WorldStatic.VanillaMovementPhysics = Config.Compatibility.VanillaMovementPhysics;
        WorldStatic.Dehacked = ArchiveCollection.Definitions.DehackedDefinition != null;
        WorldStatic.Mbf21 = Config.Compatibility.Mbf21;
        WorldStatic.Doom2ProjectileWalkTriggers = Config.Compatibility.Doom2ProjectileWalkTriggers;
        WorldStatic.OriginalExplosion = Config.Compatibility.OriginalExplosion;
        WorldStatic.FinalDoomTeleport = Config.Compatibility.FinalDoomTeleport;
        WorldStatic.VanillaSectorSound = Config.Compatibility.VanillaSectorSound;
        WorldStatic.RespawnTicks = SkillDefinition.RespawnTime.Seconds * (int)Constants.TicksPerSecond;
        WorldStatic.ClosetLookFrameIndex = ArchiveCollection.EntityFrameTable.ClosetLookFrameIndex;
        WorldStatic.ClosetChaseFrameIndex = ArchiveCollection.EntityFrameTable.ClosetChaseFrameIndex;
        WorldStatic.Udmf = MapType == MapType.UDMF;
        WorldStatic.DamageApplyMultiplier = (float)Config.Game.DamageApplyMultiplier;
        WorldStatic.DamageReceiveMultiplier = (float)Config.Game.DamageReceiveMultiplier;

        WorldStatic.DoomImpBall = EntityManager.DefinitionComposer.GetByNameOrDefault("DoomImpBall");
        WorldStatic.ArachnotronPlasma = EntityManager.DefinitionComposer.GetByNameOrDefault("ArachnotronPlasma");
        WorldStatic.Rocket = EntityManager.DefinitionComposer.GetByNameOrDefault("Rocket");
        WorldStatic.FatShot = EntityManager.DefinitionComposer.GetByNameOrDefault("FatShot");
        WorldStatic.CacodemonBall = EntityManager.DefinitionComposer.GetByNameOrDefault("CacodemonBall");
        WorldStatic.RevenantTracer = EntityManager.DefinitionComposer.GetByNameOrDefault("RevenantTracer");
        WorldStatic.RevenantTracerSmoke = EntityManager.DefinitionComposer.GetByNameOrDefault("RevenantTracerSmoke");
        WorldStatic.BaronBall = EntityManager.DefinitionComposer.GetByNameOrDefault("BaronBall");
        WorldStatic.SpawnShot = EntityManager.DefinitionComposer.GetByNameOrDefault("SpawnShot");
        WorldStatic.BFGBall = EntityManager.DefinitionComposer.GetByNameOrDefault("BFGBall");
        WorldStatic.BFGExtra = EntityManager.DefinitionComposer.GetByNameOrDefault("BFGExtra");
        WorldStatic.PlasmaBall = EntityManager.DefinitionComposer.GetByNameOrDefault("PlasmaBall");
        WorldStatic.BulletPuff = EntityManager.DefinitionComposer.GetByNameOrDefault("BulletPuff");
        WorldStatic.ArchvileFire = EntityManager.DefinitionComposer.GetByNameOrDefault("ArchvileFire");
        WorldStatic.LostSoul = EntityManager.DefinitionComposer.GetByNameOrDefault("LostSoul");
        WorldStatic.BossRocket = EntityManager.DefinitionComposer.GetByNameOrDefault("BossRocket");
        WorldStatic.RealGibs = EntityManager.DefinitionComposer.GetByNameOrDefault("RealGibs");

        WorldStatic.WeaponBfg = EntityManager.DefinitionComposer.GetByNameOrDefault(BFG900Class);
        WorldStatic.SectorFriction = false;
        WorldStatic.BloodColor = ArchiveCollection.Dehacked != null && ArchiveCollection.Dehacked.HasBloodColor;
        WorldStatic.MirrorCorpse = Config.Game.MirrorCorpse;

        if (!SameAsPreviousMap)
            WorldStatic.Sector3D = false;

        if (WorldStatic.CheckedLines.Length < Lines.Count)
            WorldStatic.CheckedLines = new int[Lines.Count];
    }

    private void VanillaSectorSound_OnChanged(object? sender, bool enabled) =>
        WorldStatic.VanillaSectorSound = enabled;
    private void FinalDoomTeleport_OnChanged(object? sender, bool enabled) =>
        WorldStatic.FinalDoomTeleport = enabled;
    private void OriginalExplosion_OnChanged(object? sender, bool enabled) =>
        WorldStatic.OriginalExplosion = enabled;
    private void Doom2ProjectileWalkTriggers_OnChanged(object? sender, bool enabled) =>
        WorldStatic.Doom2ProjectileWalkTriggers = enabled;
    private void Mbf21_OnChanged(object? sender, bool enabled) =>
       WorldStatic.Mbf21 = enabled;
    private void VanillaMovementPhysics_OnChanged(object? sender, bool enabled) =>
        WorldStatic.VanillaMovementPhysics = enabled;
    private void NoTossDrops_OnChanged(object? sender, bool enabled) =>
        WorldStatic.NoTossDrops = enabled;
    private void InfinitelyTallThings_OnChanged(object? sender, bool enabled) =>
        WorldStatic.InfinitelyTallThings = enabled;
    private void AllowItemDropoff_OnChanged(object? sender, bool enabled) =>
        WorldStatic.AllowItemDropoff = enabled;
    private void MissileClip_OnChanged(object? sender, bool enabled) =>
        WorldStatic.MissileClip = enabled;
    private void SlowTickEnabled_OnChanged(object? sender, bool enabled) =>
        WorldStatic.SlowTickEnabled = enabled;
    private void SlowTickDistance_OnChanged(object? sender, int distance) =>
        WorldStatic.SlowTickDistance = distance;
    private void SlowTickChaseFailureSkipCount_OnChanged(object? sender, int value) =>
        WorldStatic.SlowTickChaseFailureSkipCount = (short)value;
    private void SlowTickChaseMultiplier_OnChanged(object? sender, int value) =>
        WorldStatic.SlowTickChaseMultiplier = (short)value;
    private void SlowTickLookMultiplier_OnChanged(object? sender, int value) =>
        WorldStatic.SlowTickLookMultiplier = (short)value;
    private void SlowTickTracerMultiplier_OnChanged(object? sender, int value) =>
        WorldStatic.SlowTickTracerMultiplier = (short)value;
    private void DamageReceiveMultiplier_OnChanged(object? sender, double value) =>
        WorldStatic.DamageReceiveMultiplier = (float)value;
    private void DamageApplyMultiplier_OnChanged(object? sender, double value) =>
        WorldStatic.DamageApplyMultiplier = (float)value;
    private void MirrorCorpse_OnChanged(object? sender, bool enabled) => 
        WorldStatic.MirrorCorpse = enabled;
    private void FastMonsters_OnChanged(object? sender, bool enabled)
    {
        IsFastMonsters = SkillDefinition.IsFastMonsters(Config);
        WorldStatic.IsFastMonsters = IsFastMonsters;
    }

    private List<MapInfoDef> GetVisitedMaps(IList<string> visitedMaps)
    {
        List<MapInfoDef> maps = [];
        foreach (string mapName in visitedMaps)
        {
            var mapInfoDef = ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.GetMap(mapName).MapInfo;
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

    public void SetRandom(IRandom random)
    {
        WorldStatic.Random = random;
        m_random = random;
    }

    public void SetSecondaryRandom(IRandom random)
    {
        SecondaryRandom = random;
    }

    public virtual void Start(WorldModel? worldModel)
    {
        AddMapSpecial();
        InitBossBrainTargets();
        SetupMusicChangers();
        SetSectorData();

        if (!SameAsPreviousMap || worldModel == null)
        {
            SpecialManager.StartInitSpecials(LevelStats, worldModel != null);

            if (m_map is IMapSpecials mapSpecials)
                mapSpecials.Initialize(this);
        }

        SetEntityLightSectors();

        StaticDataApplier.DetermineStaticData(this);
        SpecialManager.SectorSpecialDestroyed += SpecialManager_SectorSpecialDestroyed;
    }

    private void SetEntityLightSectors()
    {
        if (!WorldStatic.Sector3D)
            return;

        // 3D sector heights are set after entities are spawned so the correct light sector needs to be recalculated here.
        for (var entity = EntityManager.Head; entity != null; entity = entity.Next)
            PhysicsManager.SetCeilingLightSector3D(entity);   
    }

    private void SetSectorData()
    {
        for (int i = 0; i < Sectors.Count; i++)
        {
            var sector = Sectors[i];
            if (!string.IsNullOrEmpty(sector.SkyFloor) &&
                GetSectorSkyTextureHandle(sector.Floor.TextureHandle, sector.SkyFloor, out var skyTextureHandle))
            {
                sector.FloorSkyTextureHandle = skyTextureHandle;
            }
            else if (GetSectorSkyTextureHandle(sector.Floor.TextureHandle, out skyTextureHandle))
            {
                sector.FloorSkyTextureHandle = skyTextureHandle;
            }

            if (!string.IsNullOrEmpty(sector.SkyCeiling) &&
                GetSectorSkyTextureHandle(sector.Ceiling.TextureHandle, sector.SkyCeiling, out skyTextureHandle))
            {
                sector.CeilingSkyTextureHandle = skyTextureHandle;
            }
            else if (GetSectorSkyTextureHandle(sector.Ceiling.TextureHandle, out skyTextureHandle))
            {
                sector.CeilingSkyTextureHandle = skyTextureHandle;
            }

            if (WorldStatic.Sector3D)
                Sector3D.SetHeights3D(sector);
        }
    }

    private bool GetSectorSkyTextureHandle(int textureHandle, out int skyTextureHandle)
    {
        skyTextureHandle = 0;
        return TextureManager.IsSkyTexture(textureHandle) && !TextureManager.IsDefaultSkyTexture(textureHandle) &&
                TextureManager.GetSkyTextureFromFlat(textureHandle, out skyTextureHandle);
    }

    private bool GetSectorSkyTextureHandle(int textureHandle, string textureName, out int skyTextureHandle)
    {
        skyTextureHandle = 0;
        if (!TextureManager.IsSkyTexture(textureHandle))
            return false;
        
        var texture = TextureManager.GetTexture(textureName, ResourceNamespace.Textures);
        if (texture == null)
            return false;

        skyTextureHandle = texture.Index;
        return true;
    }

    private void SpecialManager_SectorSpecialDestroyed(object? sender, ISectorSpecial special)
    {
        if (special is not SectorMoveSpecial move)
            return;

        SectorMoveComplete?.Invoke(this, move.SectorPlane);
    }

    public Player? GetLineOfSightPlayer(Entity entity, bool allAround)
    {
        for (int i = 0; i < EntityManager.Players.Count; i++)
        {
            Player player = EntityManager.Players[i];
            if (player.IsDead())
                continue;

            if (!allAround && !InFieldOfViewOrInMeleeDistance(entity, player))
                continue;

            if (CheckLineOfSight(entity, player))
                return player;
        }

        return null;
    }

    public Player? GetFirstAlivePlayer()
    {
        for (int i = 0; i < EntityManager.Players.Count; i++)
        {
            Player player = EntityManager.Players[i];
            if (!player.IsDead())
                return player;
        }

        return null;
    }

    public Entity? GetLineOfSightEnemy(Entity entity, bool allAround)
    {
        m_lineOfSightEnemyData.Entity = entity;
        m_lineOfSightEnemyData.AllAround = allAround;
        m_lineOfSightEnemyData.SightEntity = null;
        Box2D box = new(entity.Position.X, entity.Position.Y, 1280);
        BlockmapTraverser.EntityTraverse(box, m_lineOfSightEnemyAction);
        return m_lineOfSightEnemyData.SightEntity;
    }

    private GridIterationStatus HandleLineOfSightEnemy(Entity checkEntity)
    {
        if (m_lineOfSightEnemyData.Entity == checkEntity || checkEntity.IsDead() || !checkEntity.Flags.CountKill() ||
            m_lineOfSightEnemyData.Entity.Flags.Friendly == checkEntity.Flags.Friendly || checkEntity.IsPlayer)
            return GridIterationStatus.Continue;

        if (!m_lineOfSightEnemyData.AllAround && !InFieldOfViewOrInMeleeDistance(m_lineOfSightEnemyData.Entity, checkEntity))
            return GridIterationStatus.Continue;

        if (CheckLineOfSight(m_lineOfSightEnemyData.Entity, checkEntity))
        {
            m_lineOfSightEnemyData.SightEntity = checkEntity;
            return GridIterationStatus.Stop;
        }

        return GridIterationStatus.Continue;
    }

    public void NoiseAlert(Entity target, Entity source)
    {
        m_soundCount++;
        RecursiveSound(new(target), source.Sector, 0);
    }

    private void RecursiveSound(WeakEntity target, Sector sector, int block)
    {
        if (sector.SoundValidationCount == m_soundCount && sector.SoundBlock <= block + 1)
            return;

        sector.SoundValidationCount = m_soundCount;
        sector.SoundBlock = block + 1;
        sector.SoundTarget = target;

        int length = sector.LineIds.Length;
        var lineArray = LastStructLines.Data;
        for (int i = 0; i < length; i++)
        {
            ref var line = ref lineArray[sector.LineIds[i]];
            var frontSector = line.FrontSector;
            var backSector = line.BackSector;

            if (backSector == null)
                continue;

            double minCeilingZ = frontSector.Ceiling.Z < backSector.Ceiling.Z ? frontSector.Ceiling.Z : backSector.Ceiling.Z;
            double maxFloorZ = frontSector.Floor.Z < backSector.Floor.Z ? backSector.Floor.Z : frontSector.Floor.Z;
            if (minCeilingZ - maxFloorZ <= 0)
                continue;

            Sector other = frontSector == sector ? backSector : frontSector;
            if (line.BlockSound)
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
        Precondition(entity.SectorNodes.Length == 0 && entity.BlockRange.StartX == Constants.ClearBlock, "Forgot to unlink entity before linking");
        PhysicsManager.LinkToWorld(entity, null, false);
    }

    public void LinkClamped(Entity entity)
    {
        Precondition(entity.SectorNodes.Length == 0 && entity.BlockRange.StartX == Constants.ClearBlock, "Forgot to unlink entity before linking");
        PhysicsManager.LinkToWorld(entity, null, true);
    }

    public virtual void Tick()
    {
        if (Paused)
        {
            TickPlayerStatusBars();
            GameTicker++;
            return;
        }

        Profiler.World.Total.Start();
        OnTick?.Invoke(this, EventArgs.Empty);

        if (WorldState == WorldState.Exit)
        {
            SoundManager.Tick();
            m_exitTicks--;

            if (m_exitTicks <= 0)
            {
                LevelChangeEvent changeEvent = new(m_exitLevelArgs);
                LevelExit?.Invoke(this, changeEvent);
                if (changeEvent.Cancel)
                    WorldState = WorldState.Normal;
                else
                    WorldState = WorldState.Exited;

                m_random = m_saveRandom;
                HandleExitFlags();
            }
        }
        else if (WorldState == WorldState.Normal)
        {
            TickEntities();
            TickPlayers();
            SpecialManager.Tick();

            if (WorldState != WorldState.Exit)
            {
                if (m_changeMusicTicks > 0)
                {
                    m_changeMusicTicks--;
                    if (m_changeMusicTicks == 0 && m_lastMusicChange?.MusicData != null)
                        PlayLevelMusic(m_lastMusicChange.Name, m_lastMusicChange.MusicData);
                }

                ArchiveCollection.TextureManager.Tick();
                SoundManager.Tick();

                LevelTime++;
                GlobalData.TotalTime++;
            }
        }

        Gametick++;
        GameTicker++;

        Profiler.World.Total.Stop();
    }

    private void CreateAmbientSound(Entity entity, AmbientSoundInfo info)
    {
        var attenution = info.Type == AmbientSoundType.Point ? Attenuation.Default : Attenuation.None;
        SoundManager.CreateSoundOn(entity, info.LogicalSound, new(entity, info.Mode == AmbientSoundMode.Continuous, attenution, info.Volume, 
            attenuationFactor: info.Attenuation));
    }

    public virtual bool PlayLevelMusic(string name, byte[]? data, MusicFlags flags = MusicFlags.Loop)
    {
        m_activeMusic = name;
        return true;
    }

    protected void InvokeMusicChange(Entry entry, MusicFlags flags) => OnMusicChanged?.Invoke(this, new(entry, flags));

    private void HandleExitFlags()
    {
        if ((m_exitLevelArgs.Flags & LevelChangeFlags.KillAllPlayers) != 0)
            KillAllPlayers();

        if ((m_exitLevelArgs.Flags & LevelChangeFlags.ResetInventory) != 0)
        {
            Player.Inventory.Clear();
            Player.SetDefaultInventory();
        }

        m_exitLevelArgs.Flags = LevelChangeFlags.None;
    }

    private void TickPlayerStatusBars()
    {
        foreach (Player player in EntityManager.Players)
            player.StatusBar.Tick();
    }

    private void TickEntities()
    {
        Profiler.World.TickEntity.Start();
        var entity = EntityManager.Head;
        var nextEntity = entity;
        while (entity != null)
        {
            nextEntity = entity.Next;
            if (entity.PlayerObj != null && entity.PlayerObj.PlayerNumber == short.MaxValue)
            {
                entity = nextEntity;
                continue;
            }

            entity.Tick();

            if (WorldState == WorldState.Exit)
                break;

            // Entities can be disposed after Tick() (rocket explosion, blood spatter etc.)
            if (!entity.IsDisposed)
            {
                PhysicsManager.Move(entity);

                if (entity.Respawn)
                    HandleRespawn(entity);

                entity.Sector.SectorDamageSpecial?.Tick(entity, DamageTickOptions.CheckOnFloor);

                if (WorldStatic.Sector3D && entity.WaterControlSector != null)
                    entity.WaterControlSector.SectorDamageSpecial?.Tick(entity, DamageTickOptions.CheckWaterControlSector);

                if (!WorldStatic.InfinitelyTallThings &&
                    (entity.HadOnEntity || entity.OnEntity() != null) &&
                    !entity.Flags.NoGravity() && !entity.Flags.NoBlockmap() &&
                    entity.Velocity.Z == 0 && entity.Position.Z > entity.HighestFloorSector.Floor.Z)
                {
                    m_fallCheckEntities.Add(entity);
                }
            }

            entity = nextEntity;
        }

        // Check entities that are subject to falling and may have been on top of another entity that is no longer valid.
        // This often happens with cacodemon clusters where a dead one is on top of many and needs to fall.
        PhysicsManager.EntityFallCheck(m_fallCheckEntities);
        m_fallCheckEntities.Clear();
        Profiler.World.TickEntity.Stop();
    }

    private void TickPlayers()
    {
        Profiler.World.TickPlayer.Start();

        for (int i = 0; i < EntityManager.Players.Count; i++)
        {
            if (WorldState == WorldState.Exit)
                break;

            var player = EntityManager.Players[i];
            // Doom did not apply sector damage to voodoo dolls
            if (player.IsVooDooDoll || player.IsDisposed)
                continue;

            player.HandleTickCommand();
            player.TickCommand.TickHandled();

            if (player.Sector.Secret && player.OnSectorFloorZ(player.Sector))
            {
                player.Sector.SetSecret(false);
                PlayerSecret(player);
            }

            if (m_sectorToMusicChange.TryGetValue(player.Sector.Id, out var musInfo) && !ReferenceEquals(musInfo, m_lastMusicChange))
            {
                m_lastMusicChange = musInfo;
                m_changeMusicTicks = 30;
            }
        }

        Profiler.World.TickPlayer.Stop();
    }

    private void PlayerSecret(Player player)
    {
        DisplayMessage(player, null, "$SECRETMESSAGE");
        SoundManager.PlayStaticSound("misc/secret");
        LevelStats.SecretCount++;
        player.PlayerStats.SecretCount++;
    }

    public void SectorInstantKillEffect(Entity entity, InstantKillEffect effect)
    {
        if (!WorldStatic.Mbf21)
            return;

        // Damage rules apply for instant kill sectors. Doom did not apply sector damage to voodoo dolls
        if (entity.IsDead() || (entity.PlayerObj != null && entity.PlayerObj.IsVooDooDoll))
            return;

        if (entity.Flags.Shootable() && !entity.Flags.Float() && !entity.IsPlayer && (effect & InstantKillEffect.KillMonsters) != 0)
        {
            entity.ForceGib();
            return;
        }

        if (entity.PlayerObj == null)
            return;

        Player player = entity.PlayerObj;
        if ((effect & InstantKillEffect.KillAllPlayersExit) != 0)
            ExitLevel(ExitLevelArgs.NextMap(LevelChangeFlags.KillAllPlayers));

        if ((effect & InstantKillEffect.KillAllPlayersSecretExit) != 0)
            ExitLevel(ExitLevelArgs.NextSecretMap(LevelChangeFlags.KillAllPlayers));

        if ((effect & InstantKillEffect.KillUnprotectedPlayer) != 0 && !player.Flags.Invulnerable() &&
            !player.Inventory.IsPowerupActive(PowerupType.IronFeet))
            player.ForceGib();

        if ((effect & InstantKillEffect.KillPlayer) != 0)
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

    public virtual void Pause(PauseOptions options = PauseOptions.None)
    {
        if (Paused)
            return;

        DrawPause = (options & PauseOptions.DrawPause) != 0;
        SoundManager.Pause();

        Paused = true;
        WorldPaused?.Invoke(this, EventArgs.Empty);
    }

    public void ResetInterpolation()
    {
        for (var entity = EntityManager.Head; entity != null; entity = entity.Next)
            entity.ResetInterpolation();

        SpecialManager.ResetInterpolation();
        TextureManager.ResetInterpolation();
        OnResetInterpolation?.Invoke(this, EventArgs.Empty);
    }

    public virtual void Resume()
    {
        DrawPause = false;
        if (!Paused || DemoEnded)
            return;

        SoundManager.Resume();
        Paused = false;
        WorldResumed?.Invoke(this, EventArgs.Empty);
    }

    public void BossDeath(Entity entity)
    {
        bool anyPlayerAlive = false;
        for (int i = 0; i < EntityManager.Players.Count; i++)
        {
            if (!EntityManager.Players[i].IsDead())
            {
                anyPlayerAlive = true;
                break;
            }
        }

        if (!anyPlayerAlive)
            return;

        for (int i = 0; i < m_bossDeathSpecials.Count; i++)
        {
            var special = m_bossDeathSpecials[i];
            if (special.EntityDefinitionId == entity.Definition.Id)
                special.Tick(entity);
        }
    }

    private void AddMapSpecial()
    {
        switch (MapInfo.MapSpecial)
        {
            case MapSpecial.BaronSpecial:
                AddMonsterCountSpecial(m_bossDeathSpecials, (EntityFlags f) => f.E1M8Boss(), 666, MapInfo.MapSpecialAction);
                break;
            case MapSpecial.CyberdemonSpecial:
                AddMonsterCountSpecial(m_bossDeathSpecials, (EntityFlags f) => f.E2M8Boss() || f.E4M6Boss(), 666, MapInfo.MapSpecialAction);
                break;
            case MapSpecial.SpiderMastermindSpecial:
                AddMonsterCountSpecial(m_bossDeathSpecials, (EntityFlags f) => f.E3M8Boss() || f.E4M8Boss(), 666, MapInfo.MapSpecialAction);
                break;
            case MapSpecial.Map07Special:
                AddMonsterCountSpecial(m_bossDeathSpecials, (EntityFlags f) => f.Map07Boss1(), 666, MapSpecialAction.LowerFloor);
                AddMonsterCountSpecial(m_bossDeathSpecials, (EntityFlags f) => f.Map07Boss2(), 667, MapSpecialAction.FloorRaiseByLowestTexture);
                break;
        }

        foreach (var bossAction in MapInfo.BossActions)
        {
            var entityDef = GetBossActionDefinition(bossAction);
            if (entityDef == null)
                continue;

            m_bossDeathSpecials.Add(new BossActionMonsterCount(this, bossAction, entityDef.Id));
        }
    }

    private EntityDefinition? GetEntityDefinitionWithWarning(string definitionName, object forName)
    {
        var definition = EntityManager.DefinitionComposer.GetByName(definitionName);
        if (definition != null)
            return definition;
        HelionLog.Error($"Invalid actor name for ${forName}: {definitionName}");
        return null;
    }

    private void AddMonsterCountSpecial(List<IMonsterCounterSpecial> monsterCountSpecials, string monsterName, int sectorTag, MapSpecialAction mapSpecialAction)
    {
        var definition = GetEntityDefinitionWithWarning(monsterName, mapSpecialAction);
        if (definition == null)
            return;

        var type = mapSpecialAction switch
        {
            MapSpecialAction.LowerFloor => VanillaLineSpecialType.S1_LowerFloorToLowestAdjacentFloor,
            MapSpecialAction.OpenDoor => VanillaLineSpecialType.D1_OpenDoorStay,
            MapSpecialAction.FloorRaiseByLowestTexture => VanillaLineSpecialType.W1_RaiseFloorByShortestLowerTexture,
            MapSpecialAction.ExitLevel => VanillaLineSpecialType.S_EndLevel,
            _ => VanillaLineSpecialType.None,
        };

        if (type == VanillaLineSpecialType.None)
            return;

        m_bossDeathSpecials.Add(new BossActionMonsterCount(this, new(definition.Name, type, sectorTag), definition.Id));
    }

    private EntityDefinition? GetBossActionDefinition(BossAction bossAction)
    {
        if (bossAction.EditorNumber.HasValue)
            return ArchiveCollection.EntityDefinitionComposer.GetByID(bossAction.EditorNumber.Value);

        var translatedName = GetTranslatedDehackedName(bossAction.ActorName);
        return GetEntityDefinitionWithWarning(translatedName, "boss action");
    }

    private static string GetTranslatedDehackedName(string actorName)
    {
        const string DehActor = "Deh_Actor_";
        if (actorName.StartsWith(DehActor, StringComparison.OrdinalIgnoreCase))
        {
            string stringIndex = actorName[DehActor.Length..];
            if (!int.TryParse(stringIndex, out int index))
                return actorName;

            return DehackedApplier.GetDehackedActorName(index);
        }

        return actorName;
    }

    private IEnumerable<EntityDefinition> GetEntityDefinitionsByFlag(Func<EntityFlags, bool> isMatch)
    {
        foreach (var def in EntityManager.DefinitionComposer.GetEntityDefinitions())
            if (isMatch(def.Flags))
                yield return def;
    }

    private void AddMonsterCountSpecial(List<IMonsterCounterSpecial> monsterCountSpecials, Func<EntityFlags, bool> isMatch, int sectorTag,
        MapSpecialAction mapSpecialAction)
    {
        foreach (var def in GetEntityDefinitionsByFlag(isMatch))
            AddMonsterCountSpecial(monsterCountSpecials, def.Name, sectorTag, mapSpecialAction);
    }

    private void InitBossBrainTargets()
    {
        List<Entity> targets = new();
        for (var entity = EntityManager.Head; entity != null; entity = entity.Next)
        {
            if (entity.Definition.Name.Equals("BOSSTARGET", StringComparison.OrdinalIgnoreCase))
                targets.Add(entity);
        }

        // Doom chose for some reason to iterate in reverse order.
        targets.Reverse();
        m_bossBrainTargets = targets.ToArray();
    }

    public IList<Sector> FindBySectorTag(int tag) =>
        Geometry.FindBySectorTag(tag);

    public LinkedList<Entity> FindByTid(int tid) =>
        EntityManager.FindByTid(tid);

    public IEnumerable<Line> FindByLineId(int lineId) =>
        Geometry.FindByLineId(lineId);

    public void SetLineId(Line line, int lineId) =>
        Geometry.SetLineId(line, lineId);

    public void Dispose()
    {
        OnDestroying?.Invoke(this, EventArgs.Empty);
        SpecialManager.SectorSpecialDestroyed -= SpecialManager_SectorSpecialDestroyed;
        SoundManager.UnregisterEvents();
        PerformDispose();
        GC.SuppressFinalize(this);
    }

    const int ExitTicks = 15;

    public void ExitLevel(ExitLevelArgs args)
    {
        SoundManager.ClearSounds();
        m_exitLevelArgs = args;
        WorldState = WorldState.Exit;
        // The exit ticks thing is fudge. Change random to secondary to not break demos later.
        m_random = SecondaryRandom;
        m_exitTicks = GetExitTicks(args);
        LevelExiting?.Invoke(this, EventArgs.Empty);
    }

    private static int GetExitTicks(ExitLevelArgs args)
    {
        switch(args.Type)
        {
            case LevelChangeType.LoadNewest:
            case LevelChangeType.ResetOrLoadLast:
            case LevelChangeType.Reset:
            case LevelChangeType.SpecificLevel:
                return 0;
            default:
                return ExitTicks;
        }
    }

    public Entity[] GetBossTargets()
    {
        m_easyBossBrain ^= 1;
        if (SkillDefinition.EasyBossBrain && m_easyBossBrain == 0)
            return Array.Empty<Entity>();

        return m_bossBrainTargets;
    }

    public void TelefragBlockingEntities(Entity entity)
    {
        DynamicArray<Entity> blockingEntities = DataCache.GetEntityList();
        WorldStatic.World.BlockmapTraverser.SolidBlockTraverse(entity, entity.Position,
            !WorldStatic.InfinitelyTallThings && !WorldStatic.FinalDoomTeleport, blockingEntities, true);
        for (int i = 0; i < blockingEntities.Length; i++)
            blockingEntities[i].ForceGib();
        DataCache.FreeEntityList(blockingEntities);
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
        if (entity.IsDead())
            return false;

        bool hitBlockLine = false;
        bool activateSuccess = false;
        Vec2D start = entity.Position.XY;
        Vec2D end = start + (Vec2D.UnitCircle(entity.AngleRadians) * entity.Properties.Player.UseRange);
        var intersections = WorldStatic.Intersections;
        double openFloorZ = double.MinValue;
        double openCeilingZ = double.MaxValue;
        intersections.Clear();
        BlockmapTraverser.UseTraverse(new Seg2D(start, end), intersections);

        for (int i = 0; i < intersections.Length; i++)
        {
            ref var bi = ref intersections.Data[i];
            if (bi.GetIndex(out var lineIndex) != IntersectType.Line)
                continue;

            var line = Lines[Blockmap.BlockLines[lineIndex].LineId];
            OnTryEntityUseLine(entity, line);

            if ((entity.IsPlayer && (line.Flags.Activations & LineActivations.UseLineBack) != 0) || line.Segment.OnRight(start))
            {
                if (line.HasSpecial)
                {
                    if ((line.Flags.Activations & LineActivations.CheckSwitchRange) != 0 && !CheckSwitchRange(entity, openFloorZ, openCeilingZ))                    
                        continue;                    

                    activateSuccess = ActivateSpecialLine(entity, line, ActivationContext.UseLine, entity.Position.X, entity.Position.Y) || activateSuccess;

                    if (activateSuccess && !line.Flags.PassThrough)
                        break;
                }
            }

            if (line.Back == null || line.Flags.Blocking.Everything || line.Flags.Blocking.Use)
            {
                hitBlockLine = true;
                break;
            }

            if (line.Back != null)
            {
                var opening = PhysicsManager.GetLineOpening(line.Front.Sector, line.Back.Sector!);
                if (opening.OpeningHeight <= 0)
                {
                    hitBlockLine = true;
                    break;
                }

                if (opening.FloorZ > openFloorZ)
                    openFloorZ = opening.FloorZ;
                if (opening.CeilingZ < openCeilingZ)
                    openCeilingZ = opening.CeilingZ;

                // Keep checking if hit two-sided blocking line - this way the PlayerUserFail will be raised if no line special is hit
                if (!opening.CanPassOrStepThrough(entity))
                    hitBlockLine = true;
            }
        }

        if (!activateSuccess && hitBlockLine && entity.PlayerObj != null)
            entity.PlayerObj.PlayUseFailSound();

        return activateSuccess;
    }

    private static bool CheckSwitchRange(Entity entity, double openFloorZ, double openCeilingZ)
    {
        var bottomZ = entity.Position.Z;
        var topZ = entity.Position.Z + entity.Height;

        if (topZ < openFloorZ)
            return false;

        if (bottomZ > openCeilingZ)
            return false;

        return true;
    }

    public virtual void OnTryEntityUseLine(Entity entity, Line line)
    {

    }

    private void PlayerBumpUse(Entity entity)
    {
        if (Gametick - m_lastBumpActivateGametick < 16)
            return;

        bool shouldUse = false;
        Vec2D start = entity.Position.XY;
        Vec2D end = start + (Vec2D.UnitCircle(entity.AngleRadians) * entity.Properties.Player.UseRange);
        var intersections = WorldStatic.Intersections;
        intersections.Clear();
        BlockmapTraverser.UseTraverse(new Seg2D(start, end), intersections);

        for (int i = 0; i < intersections.Length; i++)
        {
            ref var bi = ref intersections.Data[i];
            if (bi.GetIndex(out var lineIndex) != IntersectType.Line)
                continue;

            var line = Lines[Blockmap.BlockLines[lineIndex].LineId];
            bool specialActivate = line.HasSpecial && ((line.Flags.Activations & LineActivations.UseLineBack) != 0 || line.Segment.OnRight(start));
            if (specialActivate)
                shouldUse = true;

            if (line.Back == null)
                continue;

            // This is mostly for doors. They can be reversed so ignore it if it's in motion.
            if (specialActivate && SideHasActiveMove(line.Back.Sector))
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
    }

    private static bool SideHasActiveMove(Sector sector) => sector.ActiveCeilingMove != null || sector.ActiveFloorMove != null;

    public bool CanActivate(Entity entity, Line line, ActivationContext context, double originX, double originY)
    {
        bool frontSideOnly;
        if (context == ActivationContext.UseLine)
            frontSideOnly = (line.Flags.Activations & LineActivations.UseLineBack) == 0;
        else
            frontSideOnly = (line.Flags.Activations & LineActivations.FrontSideOnly) != 0;

        if (context != ActivationContext.Always && frontSideOnly && line.Segment.PerpDot(originX, originY) > 0)
            return false;

        bool success = line.Special.CanActivate(entity, line, context,
            ArchiveCollection.Definitions.LockDefinitions, out LockDef? lockFail);
        if (entity.PlayerObj != null && lockFail != null)
        {
            entity.PlayerObj.PlayUseFailSound();
            DisplayMessage(entity.PlayerObj, null, GetLockFailMessage(line, lockFail), true);
        }
        return success;
    }

    private string GetLockFailMessage(Line line, LockDef lockDef)
    {
        if (line.Special.LineSpecialCompatibility != null &&
            line.Special.LineSpecialCompatibility.CompatibilityType == LineSpecialCompatibilityType.KeyObject)
            return ArchiveCollection.Language.GetMessage(lockDef.ObjectMessage);
        else
            return ArchiveCollection.Language.GetMessage(lockDef.DoorMessage);
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
    /// <param name="fromFront">If the line was activated from the front side.</param>
    public virtual bool ActivateSpecialLine(Entity entity, Line line, ActivationContext context, double originX, double originY)
    {
        if (!CanActivate(entity, line, context, originX, originY))
            return false;

        EntityActivateSpecial args = new(context, entity, line, line.Segment.PerpDot(originX, originY) <= 0);
        return EntityActivatedSpecial(args);
    }

    public bool GetAutoAimEntity(Entity startEntity, in Vec3D start, double angle, double distance, out double pitch, out Entity? entity) =>
        GetAutoAimAngle(startEntity, start, angle, distance, out pitch, out _, out entity, 1, 0);

    public virtual Entity? FireProjectile(Entity shooter, double angle, double pitch, double autoAimDistance, bool autoAim, EntityDefinition projectileDef, out Entity? autoAimEntity,
        double addAngle = 0, double addPitch = 0, double zOffset = 0)
    {
        autoAimEntity = null;
        Player? player = shooter.PlayerObj;
        Vec3D start = shooter.ProjectileAttackPos;
        start.Z += zOffset;

        if (autoAim && player != null &&
            GetAutoAimAngle(shooter, start, shooter.AngleRadians, autoAimDistance, out double autoAimPitch, out double autoAimAngle,
                out autoAimEntity, tracers: Constants.AutoAimTracers))
        {
            pitch = autoAimPitch;
            if (Config.Game.HorizontalAutoAim)
                angle = autoAimAngle;
        }

        pitch += addPitch;
        angle += addAngle;

        Entity projectile = EntityManager.Create(projectileDef, start, 0.0, angle, 0, default);
        // Doom set the owner as the target
        projectile.SetOwner(shooter);
        projectile.SetTarget(shooter);

        if (projectile.Flags.Randomize())
            projectile.SetRandomizeTicks();

        double speed = IsFastMonsters && projectile.Properties.FastSpeed > 0 ?
            projectile.Properties.FastSpeed : projectile.Properties.MissileMovementSpeed;

        Vec3D velocity = Vec3D.UnitSphere(angle, pitch) * speed;
        projectile.Velocity = velocity;

        projectile.PlaySeeSound(player != null
            ? new SoundContext(SoundEventType.WeaponFired, 0, ushort.MaxValue, 100)
            : default);

        if (projectile.Flags.NoClip())
            return projectile;

        Vec3D testPos = projectile.Position;
        if (projectile.Properties.MissileMovementSpeed > 0)
            testPos += Vec3D.UnitSphere(angle, pitch) * (shooter.Radius - 2.0);

        // TryMoveXY will use the velocity of the projectile
        // A projectile spawned where it can't fit can cause BlockingSectorPlane or BlockingEntity (IsBlocked = true)
        if (!projectile.IsBlocked() && PhysicsManager.TryMoveXY(projectile, testPos.X, testPos.Y).Success)
        {
            projectile.Position = testPos;
            projectile.PrevPosition = testPos;
            projectile.Velocity = velocity;
            return projectile;
        }

        projectile.Position = testPos;
        projectile.PrevPosition = testPos;
        HandleEntityHit(projectile, velocity, null);
        return null;
    }

    public virtual void FirePlayerHitscanBullets(Player shooter, int bulletCount, double spreadAngleRadians, double spreadPitchRadians, double pitch, double distance, bool autoAim,
        Func<DamageFuncParams, int>? damageFunc = null, DamageFuncParams damageParams = default)
    {
        double originalPitch = pitch;

        damageFunc ??= m_defaultDamageAction;

        if (autoAim)
        {
            Vec3D start = shooter.HitscanAttackPos;
            if (GetAutoAimAngle(shooter, start, shooter.AngleRadians, distance, out double autoAimPitch, out _, out _,
                tracers: Constants.AutoAimTracers))
            {
                pitch = autoAimPitch;
            }
        }

        if (Config.Developer.Render.Tracers && shooter.PlayerObj != null)
        {
            shooter.PlayerObj.Tracers.AddLookPath(shooter.HitscanAttackPos, shooter.AngleRadians, originalPitch, distance, Gametick);
            shooter.PlayerObj.Tracers.AddAutoAimPath(shooter.HitscanAttackPos, shooter.AngleRadians, pitch, distance, Gametick);
        }

        if (!damageParams.IgnorePlayerRefire && !shooter.Refire && bulletCount == 1)
        {
            int damage = damageFunc(damageParams);
            FireHitscan(shooter, shooter.AngleRadians, pitch, distance, damage);
            return;
        }

        for (int i = 0; i < bulletCount; i++)
        {
            int damage = damageFunc(damageParams);
            double angle = shooter.AngleRadians + (m_random.NextDiff() * spreadAngleRadians / 255);
            double newPitch = pitch + (m_random.NextDiff() * spreadPitchRadians / 255);
            FireHitscan(shooter, angle, newPitch, distance, damage);
        }
    }

    private int DefaultDamage(DamageFuncParams damageParams) => 5 * ((m_random.NextByte() % 3) + 1);

    public virtual Entity? FireHitscan(Entity shooter, double angle, double pitch, double distance, int damage,
        HitScanOptions options = HitScanOptions.Default)
    {
        var sinAngle = Math.Sin(angle);
        var cosAngle = Math.Cos(angle);
        var tanPitch = Math.Tan(pitch);
        var zOffset = tanPitch * distance;

        var intersect = Vec3D.Zero;
        var start = shooter.HitscanAttackPos;
        var end = new Vec3D(start.X + cosAngle * distance, start.Y + sinAngle * distance, start.Z + zOffset);

        var bi = FireHitScan(shooter, start, end, angle, pitch, distance, damage, options,
            tanPitch, ref intersect, out _);

        if (shooter.PlayerObj != null && (options & HitScanOptions.DrawRail) != 0)
        {
            var railEnd = bi != null && bi.Value.GetIndex(out _) == IntersectType.Line ? intersect : end;
            shooter.PlayerObj.Tracers.AddTracer(PrimitiveRenderType.Rail, (start, railEnd), Gametick, (0.2f, 0.2f, 1), 35);
        }

        if (bi == null)
            return null;

        if (bi.Value.GetIndex(out int index) == IntersectType.Entity)
            return DataCache.Entities[index];

        return null;
    }

    private void DamageMapObject(Entity entity, Line line, int damage)
    {
        var objectHealth = line.ObjectHealth;
        LineHealthGroup? group = null;
        if (line.ObjectHealth.HealthGroup > 0)
            m_lineHealthGroups.TryGetValue(line.ObjectHealth.HealthGroup, out group);

        if (!objectHealth.Damage(damage))
            return;

        bool killed = objectHealth.Health <= 0;
        if (objectHealth.DamageSpecial || killed)
        {
            ActivateSpecialLine(entity, line, ActivationContext.Always, 0, 0);
            if (group == null || !killed)
                return;

            for (int i = 0; i < group.Lines.Count; i++)
            {
                var groupLine = group.Lines[i];
                groupLine.ObjectHealth.Health = 0;
                if (line != groupLine)
                    ActivateSpecialLine(entity, groupLine, ActivationContext.Always, 0, 0);
            }
        }
    }

    public virtual BlockmapIntersect? FireHitScan(Entity shooter, Vec3D start, Vec3D end, double angle, double pitch, double distance, int damage,
        HitScanOptions options, double tanPitch, ref Vec3D intersect, out Sector? hitSector)
    {
        hitSector = null;
        BlockmapIntersect? returnValue = null;
        BlockmapIntersect? minReturnValue3D = null;
        Vec3D minIntersect3D = default;
        Sector? minHitSector3D = null;
        double minDistanceSquared3D = double.MaxValue;

        var passThrough = (options & HitScanOptions.PassThroughEntities) != 0;
        var noCrossCheck = true;
        var seg = new Seg2D(start.XY, end.XY);
        var intersections = WorldStatic.Intersections;
        intersections.Clear();
        BlockmapTraverser.ShootTraverse(seg, intersections);

        var data = intersections.Data;
        int length = intersections.Length;

        var normalSolid = shooter.IsNormalByContext(start.Z, SolidContext.HitScan);

        for (int i = 0; i < length; i++)
        {
            ref var bi = ref data[i];
            var isLine = bi.GetIndex(out var index) == IntersectType.Line;

            if (isLine)
            {
                noCrossCheck = false;
                ref var line = ref Blockmap.BlockLines[index];

                // Calculate 3D intersection point and distance for this line
                var point = seg.FromTime(bi.SegTime);
                var segDistance = bi.SegTime * distance;
                var deltaZ = tanPitch * segDistance;
                var currentDistanceSquared = segDistance * segDistance + deltaZ * deltaZ;

                // Early exit if we've already found a closer 3D hit
                if (WorldStatic.Sector3D && currentDistanceSquared > minDistanceSquared3D)
                    break;

                intersect.X = point.X;
                intersect.Y = point.Y;
                intersect.Z = start.Z + deltaZ;

                if (damage != Constants.HitscanTestDamage && line.HasSpecial)
                {
                    var mapLine = Lines[line.LineId];
                    if (mapLine.ObjectHealth != ObjectHealth.Default)
                        DamageMapObject(shooter, mapLine, damage);
                    ActivateSpecialLine(shooter, mapLine, ActivationContext.HitscanImpactsWall, shooter.Position.X, shooter.Position.Y);
                }

                double floorZ, ceilingZ;
                // One-sided wall
                if (line.BackSector == null || line.BlockFlags.Hitscan || line.BlockFlags.Everything)
                {
                    floorZ = line.FrontSector.ToFloorZ(intersect);
                    ceilingZ = line.FrontSector.ToCeilingZ(intersect);

                    if (intersect.Z > floorZ && intersect.Z < ceilingZ)
                    {
                        // Direct wall hit - this is definitely the closest
                        returnValue = bi;
                        minReturnValue3D = null;
                        break;
                    }

                    if (IsSkyClipOneSided(line.FrontSector, floorZ, ceilingZ, intersect))
                        break;

                    GetSectorPlaneIntersection(start, end, line.FrontSector, floorZ, ceilingZ, ref intersect);
                    hitSector = line.FrontSector;
                    returnValue = bi;
                    minReturnValue3D = null;
                    break;
                }

                GetOrderedSectors(line, start, out var front, out var back);

                if (line.FrontSector != line.BackSector)
                {
                    if (IsSkyClipTwoSided(front, back, intersect))
                        break;

                    floorZ = front.ToFloorZ(intersect);
                    ceilingZ = front.ToCeilingZ(intersect);
                }
                else
                {
                    // Emulate doom behavior where the line would be ignored if self referencing
                    floorZ = double.MinValue;
                    ceilingZ = double.MaxValue;
                }

                // Check 3D sector blocking
                if (WorldStatic.Sector3D)
                {
                    Vec3D test3D = default;
                    var distance3D = double.MaxValue;
                    if (SegBlockedByHitScanSector3D(front, back, start, end, intersect, ref test3D, front, ref normalSolid, ref distance3D, out var plane))
                    {
                        // Only update if this is closer than our previous best 3D hit
                        if (distance3D < minDistanceSquared3D)
                        {
                            minDistanceSquared3D = distance3D;
                            minReturnValue3D = bi;
                            if (plane != null)
                            {
                                minIntersect3D = test3D;
                                minHitSector3D = front;
                            }
                            else
                            {
                                minIntersect3D = intersect;
                                minHitSector3D = null;
                            }
                        }
                    }
                }

                // Check standard blocking
                if (intersect.Z < floorZ || intersect.Z > ceilingZ)
                {
                    GetSectorPlaneIntersection(start, end, front, floorZ, ceilingZ, ref intersect);
                    hitSector = front;
                    returnValue = bi;
                    minReturnValue3D = null;
                    break;
                }

                var opening = PhysicsManager.GetLineOpening(line.FrontSector, line.BackSector!);
                if ((floorZ != double.MinValue && opening.FloorZ > intersect.Z && intersect.Z > floorZ) ||
                    (ceilingZ != double.MaxValue && opening.CeilingZ < intersect.Z && intersect.Z < ceilingZ))
                {
                    returnValue = bi;
                    minReturnValue3D = null;
                    break;
                }

                continue;
            }

            if (!isLine && shooter.Index != index)
            {
                var entity = DataCache.Entities[index];
                if (entity.BoxIntersects(start, end, ref intersect))
                {
                    // Early exit if we've already found a closer 3D hit
                    var currentDistanceSquared = start.DistanceSquared(intersect);
                    if (WorldStatic.Sector3D && currentDistanceSquared > minDistanceSquared3D)
                        break;

                    noCrossCheck = false;
                    returnValue = bi;
                    minReturnValue3D = null;

                    if (damage != Constants.HitscanTestDamage)
                    {
                        DamageEntity(entity, shooter, damage, DamageType.AlwaysApply, Thrust.Horizontal);
                        CreateBloodOrPulletPuff(entity, intersect, angle, distance, damage);
                    }
                    if (!passThrough)
                        break;
                }
            }
        }

        if (WorldStatic.Sector3D)
        {
            // Calculate the plane intersection point of this sector and then all 3d sectors of this sector.
            // Set whichever is closest.
            Vec3D currentPlaneIntersect = default;
            GetSectorPlaneIntersection(start, end, shooter.Sector, shooter.Sector.Floor.Z, shooter.Sector.Ceiling.Z, ref currentPlaneIntersect);
            var currentDistanceSquared = start.DistanceSquared(currentPlaneIntersect);

            var distance3D = double.MaxValue;
            if (SegBlockedByHitScanSector3D(shooter.Sector, null, start, end, intersect, ref minIntersect3D, shooter.Sector, ref normalSolid, ref distance3D, out _) 
                && distance3D <= minDistanceSquared3D && distance3D <= currentDistanceSquared) 
            {
                returnValue = null;
                minReturnValue3D = new();
                minHitSector3D = shooter.Sector;
            }
            else if (noCrossCheck)
            {
                returnValue = new();
                hitSector = shooter.Sector;
                intersect = currentPlaneIntersect;
            }
        }
        else if (noCrossCheck && returnValue == null)
        {
            hitSector = shooter.Sector;
            returnValue = new();
            GetSectorPlaneIntersection(start, end, shooter.Sector, shooter.Sector.Floor.Z, shooter.Sector.Ceiling.Z, ref intersect);
        }

        // Apply deferred 3D hit if it was the closest and we didn't find a concrete blocker
        if (minReturnValue3D != null && returnValue == null)
        {
            returnValue = minReturnValue3D;
            intersect = minIntersect3D;
            hitSector = minHitSector3D;
        }

        if (returnValue != null && damage > 0)
        {
            // Only move closer on a line hit
            bool isLine = returnValue.Value.GetIndex(out var index) == IntersectType.Line;
            if (isLine && hitSector == null)
                MoveIntersectCloser(start, ref intersect, angle, returnValue.Value.SegTime * distance);
            CreateBloodOrPulletPuff(isLine ? null : DataCache.Entities[index], intersect, angle, distance, damage);
        }

        return returnValue;
    }

    private bool SegBlockedByHitScanSector3D(Sector front, Sector? back, in Vec3D start, in Vec3D end, in Vec3D intersect, ref Vec3D hitIntersect, 
        Sector frontSector, ref bool normalSolid, ref double minDistanceSquared3D, out SectorPlane? plane, bool earlyExit = false)
    {
        Vec3D minHit = default;
        plane = null;
        SectorPlane? hitPlane = null;

        CheckForBlockedHitScanSector3D(start, end, intersect, frontSector, normalSolid, ref minHit, ref hitPlane, ref minDistanceSquared3D, front, earlyExit, out var hitSector3D);
        if (back != null)
            CheckForBlockedHitScanSector3D(start, end, intersect, frontSector, normalSolid, ref minHit, ref hitPlane, ref minDistanceSquared3D, back, earlyExit, out hitSector3D);

        if (hitSector3D != null)
            normalSolid = (hitSector3D.Flags & SectorFlags3D.ShootInvert) == 0;

        if (minDistanceSquared3D != double.MaxValue)
        {
            hitIntersect = minHit;
            plane = hitPlane;
            return true;
        }

        return false;
    }

    private void CheckForBlockedHitScanSector3D(in Vec3D start, in Vec3D end, in Vec3D intersect, Sector frontSector,
        bool normalSolid, ref Vec3D minHit, ref SectorPlane? hitPlane, ref double minDistanceSquared3D, Sector sector, bool earlyExit, out Sector3D? hitSector3D)
    {
        hitSector3D = null;
        Vec3D test = default;
        for (int i = 0; i < sector.Sectors3D.Length; i++)
        {
            var sector3D = sector.Sectors3D[i];

            if (!normalSolid)
            {
                if ((sector3D.Flags & SectorFlags3D.ShootInvert) != 0)
                    continue;
            }
            else
            {
                if (!sector3D.IsSolidByContext(SolidContext.HitScan))
                    continue;
            }

            if (frontSector != sector && sector3D.ControlBottom.Z <= intersect.Z && sector3D.ControlTop.Z >= intersect.Z)
            {
                if (earlyExit)
                {
                    hitSector3D = sector3D;
                    minHit = intersect;
                    minDistanceSquared3D = 0;
                    return;
                }

                var distance = start.DistanceSquared(intersect);
                if (distance < minDistanceSquared3D)
                {
                    hitSector3D = sector3D;
                    minHit = intersect;
                    minDistanceSquared3D = distance;
                }
            }

            if (IntersectPlane3D(sector3D, sector, start, end, ref test, out var testPlane))
            {
                if (earlyExit)
                {
                    hitPlane = testPlane;
                    minHit = test;
                    minDistanceSquared3D = 0;
                    return;
                }

                var distance = start.DistanceSquared(test);
                if (distance <= minDistanceSquared3D)
                {
                    hitPlane = testPlane;
                    minHit = test;
                    minDistanceSquared3D = distance;
                }
            }
        }
    }

    private bool IntersectPlane3D(Sector3D sector3D, Sector sector, in Vec3D start, in Vec3D end, ref Vec3D intersect, out SectorPlane? plane)
    {
        plane = null;

        var checkTopZ = sector3D.ControlTop.Z;
        var checkBottomZ = sector3D.ControlBottom.Z;
        if (start.Z <= sector3D.ControlTop.Z && start.Z >= sector3D.ControlBottom.Z)
            (checkTopZ, checkBottomZ) = (checkBottomZ, checkTopZ);

        if (start.Z < checkBottomZ && sector3D.ControlBottom.Plane.Intersects(start, end, ref intersect) && PointInSector(sector, intersect))
        {
            plane = sector3D.ControlBottom;
            return true;
        }

        if (start.Z > checkTopZ && sector3D.ControlTop.Plane.Intersects(start, end, ref intersect) && PointInSector(sector, intersect))
        {
            plane = sector3D.ControlTop;
            return true;
        }

        return false;
    }

    private bool PointInSector(Sector sector, in Vec3D point)
    {
        var subsector = ToSubsector(point.X, point.Y);
        return subsector.Sector == sector;
    }

    public virtual bool DamageEntity(Entity target, Entity? source, int damage, DamageType damageType,
        Thrust thrust = Thrust.HorizontalAndVertical, Sector? sectorSource = null)
    {
        if (source != null && source.Owner() == target)
            damage = (int)(damage * source.Properties.SelfDamageFactor);

        if (!target.Flags.Shootable() || target.Flags.Dormant() || damage == 0 || target.IsDead())
            return false;

        Vec3D thrustVelocity = Vec3D.Zero;
        if (source != null && thrust != Thrust.None)
        {
            Vec3D savePos = source.Position;
            // Check if the source is owned by this target and the same position and move to get a valid thrust angle. (player shot missile against wall)
            if (source.Owner() == target && source.Position.XY == target.Position.XY)
            {
                Vec3D move = (source.Position.XY + Vec2D.UnitCircle(target.AngleRadians) * 2).To3D(source.Position.Z);
                source.Position = move;
            }

            var angle = source.Position.Angle(target.Position);
            var thrustAmount = damage * source.ProjectileKickBack * 0.125 / target.Properties.Mass;

            // Silly vanilla doom feature that allows target to be thrown forward sometimes
            if (damage < 40 && damage > target.Health &&
                target.Position.Z - source.Position.Z > 64 && (m_random.NextByte() & 1) != 0)
            {
                angle += Math.PI;
                thrustAmount *= 4;
            }

            if (thrust == Thrust.HorizontalAndVertical)
            {
                var pitch = 0.0;
                var zEqual = Math.Abs(target.Position.Z - source.Position.Z) <= double.Epsilon;
                var xyEqual = Math.Abs(source.Position.X - target.Position.X) <= 1.0 && Math.Abs(source.Position.Y - target.Position.Y) <= 1.0;
                // Player rocket jumping check, back up the source Z to get a valid pitch
                // Only done for players, otherwise blowing up enemies will launch them in the air
                if (zEqual && target.IsPlayer && source.Owner() == target)
                {
                    var sourcePos = new Vec3D(source.Position.X, source.Position.Y, source.Position.Z - 1.0);
                    pitch = sourcePos.Pitch(target.Position, 0.0);
                }
                else if (source.Position.Z < target.Position.Z || source.Position.Z > target.Position.Z + target.Height)
                {
                    var sourcePos = source.CenterPoint;
                    var targetPos = target.Position;
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
                source.Position = savePos;
        }

        var setPainState = m_random.NextByte() < target.Properties.PainChance;
        if (target.PlayerObj != null)
        {
            damage = (int)(damage * WorldStatic.DamageReceiveMultiplier);
            // Voodoo dolls did not take sector damage in the original
            if (target.PlayerObj.IsVooDooDoll && sectorSource != null)
                return false;
            // Sector damage is applied to real players, but not their voodoo dolls
            if (sectorSource == null)
                ApplyVooDooDamage(target.PlayerObj, damage, setPainState);
        }
        else if (source?.PlayerObj != null)
        {
            damage = (int)(damage * WorldStatic.DamageApplyMultiplier);
        }

        if (target.Damage(source, damage, setPainState, damageType) || target.IsInvulnerable)
            target.Velocity += thrustVelocity;

        return true;
    }

    public virtual bool GiveItem(Player player, Entity item, EntityFlags? flags, out EntityDefinition definition, bool pickupFlash = true)
    {
        if (!item.Definition.IgnoreVanillaSpriteLookup &&
            ArchiveCollection.Definitions.DehackedDefinition != null &&
            GetDehackedPickup(ArchiveCollection.Definitions.DehackedDefinition, item, out var vanillaDef))
        {
            definition = vanillaDef;
            flags = GetCombinedPickupFlags(vanillaDef.Flags, flags);
            return GiveItemInternal(player, vanillaDef, flags, pickupFlash);
        }
        else if (item.Definition.Properties.TranslatedPickups != null)
        {
            bool success = false;
            definition = item.Definition.Properties.TranslatedPickupDisplay ?? item.Definition;
            foreach (var pickupDef in item.Definition.Properties.TranslatedPickups)
            {
                var pickupFlags = GetCombinedPickupFlags(pickupDef.Flags, flags);
                if (GiveItemInternal(player, pickupDef, pickupFlags, pickupFlash))
                    success = true;
            }

            return success;
        }

        definition = item.Definition;
        return GiveItemInternal(player, definition, flags, pickupFlash);
    }

    private bool GiveItemInternal(Player player, EntityDefinition pickupDef, EntityFlags? flags, bool pickupFlash)
    {
        if (player.IsVooDooDoll)
            return GiveVooDooItem(player, pickupDef, flags, pickupFlash);

        return player.GiveItem(pickupDef, flags, pickupFlash);
    }

    private static EntityFlags GetCombinedPickupFlags(EntityFlags dehackedFlags, EntityFlags? flags)
    {
        // Need to carry over flags that are modified by the world and affect pickups
        if (flags.HasValue)
        {
            dehackedFlags.SetDropped(flags.Value.Dropped());
            dehackedFlags.SetSpecialStaySingle(flags.Value.SpecialStaySingle());
            dehackedFlags.SetSpecialStayCooperative(flags.Value.SpecialStayCooperative());
            dehackedFlags.SetSpecialStayDeathmatch(flags.Value.SpecialStayDeathmatch());
        }

        return dehackedFlags;
    }

    private bool GetDehackedPickup(DehackedDefinition dehacked, Entity item, [NotNullWhen(true)] out EntityDefinition? definition)
    {
        // Vanilla determined pickups by the sprite name
        // E.g. batman doom has an enemy that drops a shotgun with the blue key sprite
        if (!dehacked.PickupLookup.TryGetValue(item.FrameState.Frame.Sprite, out string? def))
        {
            definition = null;
            return false;
        }

        definition = ArchiveCollection.EntityDefinitionComposer.GetByName(def);
        return definition != null;
    }

    public virtual void PerformItemPickup(Entity entity, Entity item)
    {
        if (entity.PlayerObj == null)
            return;

        var shouldStay = ShouldItemStay(item);
        if (shouldStay && entity.PlayerObj.HasItemOrWeapon(item.Definition))
            return;

        int health = entity.PlayerObj.Health;
        if (!GiveItem(entity.PlayerObj, item, item.Flags, out EntityDefinition definition))
            return;

        if (item.IsDisposed)
            return;

        if (entity.PlayerObj != null)
            PlayerPickedUpItem(entity.PlayerObj, item, health, definition);

        if (!shouldStay)
        {
            ActivateEntitySpecial(item);
            EntityManager.Destroy(item);
        }
    }

    private void ActivateEntitySpecial(Entity entity)
    {
        if (entity.Special != ZDoomLineSpecialType.None)
            SpecialManager.AddActivatedLineSpecial(entity.Special, entity.Args);
    }

    public virtual bool ShouldItemStay(Entity item)
    {
        return WorldType switch
        {
            WorldType.Cooperative => item.Flags.SpecialStayCooperative() || ShouldItemStayMultiplayer(item),
            WorldType.Deathmatch => item.Flags.SpecialStayDeathmatch() || ShouldItemStayMultiplayer(item),
            _ => item.Flags.SpecialStaySingle(),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldItemStayMultiplayer(Entity item) =>
        item.Definition.IsType(Inventory.KeyClassName) || (item.Definition.IsType(Inventory.WeaponClassName) && !item.Flags.Dropped());

    private void PlayerPickedUpItem(Player player, Entity item, int previousHealth, EntityDefinition definition)
    {
        if (player.IsVooDooDoll)
        {
            var findPlayer = EntityManager.GetRealPlayer(player.PlayerNumber);
            if (findPlayer == null)
                return;
            player = findPlayer;
        }

        // TODO
        m_itemPickupIndexToPlayers[item.Index] = player;
        item.FrameState.SetState(item, item.Definition, Constants.FrameStates.Pickup, warn: false);
        m_itemPickupIndexToPlayers.Remove(item.Index);

        if (item.Flags.CountItem())
        {
            LevelStats.ItemCount++;
            player.PlayerStats.ItemCount++;
        }

        string message = definition.Properties.Inventory.PickupMessage;
        var healthProperty = definition.Properties.HealthProperty;
        if (healthProperty != null && previousHealth < healthProperty.Value.LowMessageHealth && healthProperty.Value.LowMessage.Length > 0)
            message = healthProperty.Value.LowMessage;

        DisplayMessage(player, null, message);

        if (!string.IsNullOrEmpty(definition.Properties.Inventory.PickupSound))
        {
            SoundManager.CreateSoundOn(player, definition.Properties.Inventory.PickupSound,
                new SoundParams(player, channel: SoundChannel.Item));
        }

        if (item.Flags.CountSecret())
        {
            PlayerSecret(Player);
            item.Flags.ClearCountSecret();
        }
    }

    public virtual void HandleEntityHit(Entity entity, in Vec3D previousVelocity, TryMoveData? tryMove)
    {
        if (entity.IsDisposed)
            return;

        entity.Hit(previousVelocity);

        if (tryMove != null && (entity.Flags.Missile() || entity.Flags.CountKill() || entity.IsPlayer))
        {
            for (int i = 0; i < tryMove.ImpactSpecialLines.Length; i++)
            {
                var line = Lines[tryMove.ImpactSpecialLines[i]];
                ActivateSpecialLine(entity, line, ActivationContext.EntityImpactsWall, entity.Position.X, entity.Position.Y);
            }

            if (entity.PlayerObj != null && !entity.PlayerObj.IsVooDooDoll && Config.Game.BumpUse)
                PlayerBumpUse(entity);
        }

        if (entity.ShouldDieOnCollision())
        {
            if (entity.BlockingEntity != null || (tryMove != null && tryMove.ImpactSpecialLines.Length > 0))
            {
                int damage = entity.Properties.Damage.Get(m_random);
                if (entity.BlockingEntity != null)
                    DamageEntity(entity.BlockingEntity, entity, damage, DamageType.Normal);

                if (tryMove != null)
                {
                    for (int i = 0; i < tryMove.ImpactSpecialLines.Length; i++)
                    {
                        var line = Lines[tryMove.ImpactSpecialLines[i]];
                        if (line.ObjectHealth == ObjectHealth.Default)
                            continue;
                        DamageMapObject(entity, line, damage);
                    }
                }
            }

            bool skyClip = false;
            if (entity.BlockingBlockLineIndex != -1)
            {
                ref var line = ref Blockmap.BlockLines[entity.BlockingBlockLineIndex];
                if (line.BackSector != null)
                {
                    GetOrderedSectors(line, entity.Position, out var front, out var back);
                    if (IsSkyClipTwoSided(front, back, entity.Position))
                        skyClip = true;
                }
            }

            if (entity.BlockingSectorPlane != null && ArchiveCollection.TextureManager.IsSkyTexture(entity.BlockingSectorPlane.TextureHandle))
                skyClip = true;

            if (skyClip)
                EntityManager.Destroy(entity);
            else
                entity.SetDeathState(null, DamageType.Normal);
        }
        else if (entity.Flags.Touchy() || (entity.BlockingEntity != null && entity.BlockingEntity.Flags.Touchy()))
        {
            if (entity.BlockingEntity != null && ShouldDieFromTouch(entity, entity.BlockingEntity))
                entity.BlockingEntity.Kill(null);
        }

        if (entity.BlockingBlockLineIndex != -1 &&
            entity.WaterSubmersionLevel != SubmersionLevel.None && entity.WaterSubmersionLevel < SubmersionLevel.Full && 
            entity.IsPlayer && entity.HasMovementXY)
        {
            ref var line = ref Blockmap.BlockLines[entity.BlockingBlockLineIndex];
            if (line.BackSector != null)
                entity.Velocity.Z = 3.8;
        }
    }

    public virtual void HandleEntityClipPlane(Entity entity, SectorPlane plane)
    {
        if (entity.Flags.Touchy())
            entity.Kill(null);
    }

    public virtual void HandleEntityIntersections(Entity entity, in Vec3D previousVelocity, TryMoveData? tryMove)
    {
        if (tryMove == null || tryMove.IntersectEntities2D.Length == 0)
            return;

        for (int i = 0; i < tryMove.IntersectEntities2D.Length; i++)
        {
            var intersectEntity = tryMove.IntersectEntities2D[i];
            if (!entity.OverlapsZ(intersectEntity) || entity == intersectEntity)
                continue;

            if (intersectEntity.Flags.Touchy() && ShouldDieFromTouch(entity, intersectEntity))
                intersectEntity.Kill(null);
        }
    }

    public virtual void HandleFinalizeEntityIntersections(Entity entity, TryMoveData? tryMove)
    {
        if (tryMove == null || tryMove.IntersectEntities2D.Length == 0 || !entity.Flags.Ripper())
            return;

        for (int i = 0; i < tryMove.IntersectEntities2D.Length; i++)
        {
            var intersectEntity = tryMove.IntersectEntities2D[i];
            if (!entity.OverlapsZ(intersectEntity) || entity == intersectEntity)
                continue;

            if (entity.Owner() != intersectEntity)
                RipDamage(entity, intersectEntity);
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
        if (!blockingEntity.Flags.Touchy() || !entity.Flags.Solid() || !blockingEntity.CanDamage(entity, DamageType.Normal))
            return false;

        if (entity.Definition.IsType(painElemental) && blockingEntity.Definition.IsType(lostSoul))
            return false;

        if (entity.Definition.IsType(lostSoul) && blockingEntity.Definition.IsType(painElemental))
            return false;

        return true;
    }

    public void SetLineOfSightDistance(int length) => m_losDistance = length;

    public virtual bool CheckLineOfSight(Entity from, Entity to)
    {
        if (m_lineOfSightReject.Length > 0 && IsLineOfSightRejected(from, to))
            return false;

        var start = new Vec2D(from.Position.X, from.Position.Y);
        var end = new Vec2D(to.Position.X, to.Position.Y);

        if (start.X == end.X && start.Y == end.Y && !WorldStatic.Sector3D)
            return true;

        if (from.Sector.TransferHeights != null && TransferHeightsLineOfSightBlocked(from, to, from.Sector.TransferHeights))
            return false;
        if (to.Sector.TransferHeights != null && TransferHeightsLineOfSightBlocked(to, from, to.Sector.TransferHeights))
            return false;

        Vec3D sightPos = new(from.Position.X, from.Position.Y, from.Position.Z + (from.Height * 0.75));
        Vec3D endSightPos = to.Position;
        var normalSolid = from.IsNormalByContext(sightPos.Z, SolidContext.LineOfSight);

        if (start.X == end.X && start.Y == end.Y && WorldStatic.Sector3D)
            return CheckLineOfSightPlane3D(from, to, sightPos, endSightPos, ref normalSolid);

        bool hitOneSidedLine;
        var seg = new Seg2D(start, end);
        var segLength = seg.Length();
        var intersections = WorldStatic.Intersections;

        var topSlope = (endSightPos.Z + to.Height - sightPos.Z) / segLength;
        var bottomSlope = (endSightPos.Z - sightPos.Z) / segLength;

        if (WorldStatic.Sector3D || segLength <= m_losDistance)
        {
            BlockmapTraverser.SightTraverse(seg, intersections, out hitOneSidedLine);
            if (hitOneSidedLine)
                return false;

            var status = GetBlockmapTraversalPitch(intersections, sightPos, from, segLength, normalSolid, SolidContext.LineOfSight, ref topSlope, ref bottomSlope, out _, out _, out var crossedLine);
            if (!WorldStatic.Sector3D || status == TraversalPitchStatus.Blocked)
                return status != TraversalPitchStatus.Blocked;

            if (m_pitchOnBlockLine != -1 || !crossedLine && !CheckLineOfSightPlane3D(from, to, sightPos, endSightPos, ref normalSolid))
                return false;

            if (m_pitchOnBlockLine == -1)
                return true;

            // Entity on line can produce false positives and leak through blocking 3D sector planes.
            ref var segStart = ref seg.Start;
            segStart.X += 1;
            segStart.Y += 1;
            BlockmapTraverser.SightTraverse(seg, intersections, out hitOneSidedLine);
            if (hitOneSidedLine)
                return false;

            sightPos.X = segStart.X;
            sightPos.Y = segStart.Y;
            status = GetBlockmapTraversalPitch(intersections, sightPos, from, segLength, normalSolid, SolidContext.LineOfSight, ref topSlope, ref bottomSlope, out _, out _, out _);
            return status != TraversalPitchStatus.Blocked;
        }

        // A lot of LOS checks on large maps will short early. Check the first sorted set, and then rest if it passes.
        double segTime = m_losDistance / segLength;
        seg = new Seg2D(start, seg.FromTime(segTime));
        double sliceSegLength = m_losDistance;
        BlockmapTraverser.SightTraverse(seg, intersections, out hitOneSidedLine);
        if (hitOneSidedLine)
            return false;

        if (GetBlockmapTraversalPitch(intersections, sightPos, from, sliceSegLength, normalSolid, SolidContext.LineOfSight, ref topSlope, ref bottomSlope, out _, out _, out _) == TraversalPitchStatus.Blocked)
            return false;

        seg = new Seg2D(seg.End, end);
        sliceSegLength = segLength - m_losDistance;
        BlockmapTraverser.SightTraverse(seg, intersections, out hitOneSidedLine);
        if (hitOneSidedLine)
            return false;

        var slice = new Vec3D(sightPos.X + ((endSightPos.X - sightPos.X) * segTime), sightPos.Y + ((endSightPos.Y - sightPos.Y) * segTime),
            sightPos.Z + ((endSightPos.Z - sightPos.Z) * segTime));
        return GetBlockmapTraversalPitch(intersections, slice, from, sliceSegLength, normalSolid, SolidContext.LineOfSight, ref topSlope, ref bottomSlope, out _, out _, out _) != TraversalPitchStatus.Blocked;
    }

    private bool CheckLineOfSightPlane3D(Entity from, Entity to, Vec3D sightPos, Vec3D endSightPos, ref bool normalSolid)
    {
        var test = double.MaxValue;
        Vec3D ignoreRange = new(0, 0, double.MinValue);
        Vec3D ignoreSet = default;

        // Validate sight segments against the sector that the entities are in.
        // This fixes cases where nothing between start and end when entity is at same x/y but different 3D sector plane.
        // Early exit is set as since order doesn't matter, just if it's blocked.
        if (SegBlockedByHitScanSector3D(from.Sector, null, sightPos, endSightPos, ignoreRange, ref ignoreSet, from.Sector, ref normalSolid, ref test, out _, earlyExit: true))
            return false;

        if (SegBlockedByHitScanSector3D(to.Sector, null, sightPos, endSightPos, ignoreRange, ref ignoreSet, to.Sector, ref normalSolid, ref test, out _, earlyExit: true))
            return false;

        return true;
    }

    private static bool TransferHeightsLineOfSightBlocked(Entity from, Entity to, TransferHeights heights)
    {
        var sector = heights.ControlSector;
        return (from.Position.Z + from.Height <= sector.Floor.Z && to.Position.Z >= sector.Floor.Z) ||
               (from.Position.Z >= sector.Ceiling.Z && to.Position.Z + to.Height <= sector.Ceiling.Z);
    }

    private bool CheckLineOfSight(Entity from, in BlockLine line)
    {
        if (line.Segment.OnRight(from.Position))
            return false;

        var fromPos = from.Position.XY;
        var closestPoint = line.Segment.ClosestPoint(fromPos);
        if (fromPos.DistanceSquared(closestPoint) > m_radiusExplosion.MaxDamage * m_radiusExplosion.MaxDamage)
            return false;

        Sector front;
        Sector? back = null;
        if (line.OneSided)
            front = line.FrontSector;
        else
            GetOrderedSectors(line, from.Position, out front, out back);

        double floorZ, ceilingZ;
        if (back == null)
        {
            floorZ = line.FrontSector.Floor.Z;
            ceilingZ = line.FrontSector.Ceiling.Z;
        }
        else
        {
            floorZ = front.Floor.Z;
            ceilingZ = front.Ceiling.Z;
        }

        m_checkRadiusEntity.Position.X = closestPoint.X;
        m_checkRadiusEntity.Position.Y = closestPoint.Y;
        m_checkRadiusEntity.Position.Z = floorZ;
        m_checkRadiusEntity.Height = ceilingZ - floorZ;

        var success = CheckLineOfSight(from, m_checkRadiusEntity);
        return success;
    }

    private bool IsLineOfSightRejected(Entity from, Entity to)
    {
        int pnum = from.Sector.Id * Sectors.Count + to.Sector.Id;
        int bytenum = pnum >> 3;

        if (m_lineOfSightReject.Length <= bytenum)
            return false;

        if ((m_lineOfSightReject[bytenum] & (1 << (pnum & 7))) != 0)
            return true;

        return false;
    }

    public virtual bool InFieldOfView(Entity from, Entity to, double fieldOfViewRadians)
    {
        Vec2D entityLookingVector = Vec2D.UnitCircle(from.AngleRadians);
        Vec2D entityToTarget = new(to.Position.X - from.Position.X, to.Position.Y - from.Position.Y);
        entityToTarget.Normalize();
        var angle = Math.Acos(entityToTarget.Dot(entityLookingVector));
        return angle < fieldOfViewRadians / 2;
    }

    private static bool InFieldOfViewOrInMeleeDistance(Entity from, Entity to)
    {
        Vec2D entityLookingVector = Vec2D.UnitCircle(from.AngleRadians);
        Vec2D entityToTarget = new(to.Position.X - from.Position.X, to.Position.Y - from.Position.Y);

        // Not in front 180 FOV
        if (entityToTarget.Dot(entityLookingVector) < 0 && from.Position.ApproximateDistance2D(to.Position) > Constants.EntityMeleeDistance)
            return false;

        return true;
    }

    public virtual void RadiusExplosion(Entity damageSource, Entity attackSource, int radius, int maxDamage)
    {
        m_radiusExplosion.DamageSource = damageSource;
        m_radiusExplosion.AttackSource = attackSource;
        m_radiusExplosion.Radius = radius;
        m_radiusExplosion.MaxDamage = maxDamage;
        m_radiusExplosion.Thrust = damageSource.Flags.OldRadiusDmg() ? Thrust.Horizontal : Thrust.HorizontalAndVertical;
        Vec2D pos2D = damageSource.Position.XY;
        Vec2D radius2D = new(radius, radius);
        Box2D explosionBox = new(pos2D - radius2D, pos2D + radius2D);

        if (m_explosionTraverseLines)
            BlockmapTraverser.ExplosionTraverseWithLines(explosionBox, m_radiusExplosionEntityAction, m_radiusExplosionLineAction);
        else
            BlockmapTraverser.ExplosionTraverse(explosionBox, m_radiusExplosionEntityAction);
    }

    private void HandleRadiusExplosionEntity(Entity entity)
    {
        if (!ShouldApplyExplosionDamage(entity, m_radiusExplosion.DamageSource))
            return;

        ApplyExplosionDamageAndThrust(m_radiusExplosion.DamageSource, m_radiusExplosion.AttackSource, entity,
            m_radiusExplosion.Radius, m_radiusExplosion.MaxDamage, m_radiusExplosion.Thrust,
            WorldStatic.OriginalExplosion || m_radiusExplosion.DamageSource.Flags.OldRadiusDmg() || entity.Flags.OldRadiusDmg());
    }

    private void HandleRadiusExplosionLine(int blockLineIndex)
    {
        int lineId = Blockmap.BlockLines[blockLineIndex].LineId;
        var line = Lines[lineId];

        if (line.ObjectHealth == ObjectHealth.Default || line.ObjectHealth.Health <= 0)
            return;

        ref var blockLine = ref Blockmap.BlockLines[blockLineIndex];
        if (!CheckLineOfSight(m_radiusExplosion.DamageSource, blockLine))
            return;

        var applyDamage = CalcRadiusExplosionDamage(m_radiusExplosion.DamageSource, m_checkRadiusEntity, m_radiusExplosion.Radius, m_radiusExplosion.MaxDamage, Thrust.None, true);
        DamageMapObject(m_radiusExplosion.AttackSource, line, applyDamage);
    }

    private bool ShouldApplyExplosionDamage(Entity entity, Entity damageSource)
    {
        if ((entity.Flags.Boss() || entity.Flags.NoRadiusDmg()) && !damageSource.Flags.ForceRadiusDmg())
            return false;

        if (!entity.CanApplyRadiusExplosionDamage(damageSource) || !CheckLineOfSight(entity, damageSource))
            return false;

        return true;
    }

    public virtual TryMoveData TryMoveXY(Entity entity, Vec2D position)
        => PhysicsManager.TryMoveXY(entity, position.X, position.Y);

    public virtual bool IsPositionValid(Entity entity, Vec2D position) =>
        PhysicsManager.IsPositionValid(entity, position.X, position.Y);

    public virtual SectorMoveStatus MoveSectorZ(double speed, double destZ, SectorMoveSpecial moveSpecial)
    {
        if (moveSpecial.IsInitialMove)
            SectorMoveStart?.Invoke(this, moveSpecial.SectorPlane);

        var status = PhysicsManager.MoveSectorZ(speed, destZ, moveSpecial, moveSpecial.Sector);
        SectorMove?.Invoke(this, moveSpecial.SectorPlane);
        return status;
    }

    public virtual void HandleEntityDeath(Entity deathEntity, Entity? deathSource, DamageType damageType, bool gibbed)
    {
        CheckDropItem(deathEntity);

        if (deathEntity.Flags.CountKill())
        {
            var player = deathSource?.PlayerObj;

            if (!deathEntity.Flags.Friendly())
            {
                LevelStats.KillCount++;
                if (player != null)
                    player.PlayerStats.MonsterKillCount++;
            }
            else
            {
                if (player != null)
                    player.PlayerStats.FriendlyKillCount++;
            }
        }

        if (deathEntity.PlayerObj != null)
        {
            HandleObituary(deathEntity.PlayerObj, deathSource, damageType);
            ApplyVooDooKill(deathEntity.PlayerObj, deathSource, gibbed);
        }

        ActivateEntitySpecial(deathEntity);
    }

    private void CheckDropItem(Entity deathEntity)
    {
        ref var dropItemDef = ref deathEntity.Definition.Properties.DropItem;
        if (dropItemDef != null &&
            (dropItemDef.Value.Probability == DropItemProperty.DefaultProbability ||
                m_random.NextByte() < dropItemDef.Value.Probability))
        {
            for (int i = 0; i < dropItemDef.Value.Amount; i++)
            {
                bool initSpawn = true;
                Vec3D pos = deathEntity.Position;
                pos.Z = deathEntity.Sector.Floor.Z;
                double addVelocity = 0;
                if (!WorldStatic.NoTossDrops)
                {
                    initSpawn = false;
                    pos.Z = deathEntity.Position.Z + deathEntity.Definition.Properties.Height / 2;
                    addVelocity = 4;
                }

                Entity? dropItem = EntityManager.Create(dropItemDef.Value.ClassName, pos, initSpawn: initSpawn);
                if (dropItem == null)
                    continue;

                dropItem.Flags.SetDropped();
                dropItem.Velocity.Z += addVelocity;
            }
        }
    }

    private void HandleObituary(Player player, Entity? deathSource, DamageType damageType)
    {
        if (ArchiveCollection.IWadType == IWadBaseType.ChexQuest)
            return;

        string? obituary = null;
        Entity? killer = null;
        if (deathSource != null)
        {
            // If the player killed themself then don't display the obituary message
            // There is probably a special string for this in multiplayer for later
            killer = deathSource.Owner() ?? deathSource;
            if (player == killer)
                return;

            // Monster obituaries can come from the projectile, while the player obituaries always come from the owner player
            var obituarySource = killer;
            if (killer.IsPlayer)
                obituarySource = deathSource;

            if (obituarySource == deathSource && obituarySource.Definition.Properties.HitObituary.Length > 0)
                obituary = obituarySource.Definition.Properties.HitObituary;
            else
                obituary = obituarySource.Definition.Properties.Obituary;
        }

        if (damageType == DamageType.Drowning)
            obituary = "$OB_WATER";

        if (!string.IsNullOrEmpty(obituary))
            DisplayMessage(player, killer?.PlayerObj, obituary);
    }

    public virtual void DisplayMessage(string message, bool isCentered = false) => DisplayMessage(null, null, message, isCentered);

    public virtual void DisplayMessage(Player? player, Player? other, string message, bool isCentered = false)
    {
        message = ArchiveCollection.Definitions.Language.GetMessage(player, other, message);
        if (message.Length > 0)
        {
            if (!isCentered && (player == null || player == GetCameraPlayer()))
                HelionLog.Info(message);
            if (player != null && player == GetCameraPlayer())
                PlayerMessage?.Invoke(this, new PlayerMessageEvent(player, message, isCentered));
        }
    }

    private void HandleRespawn(Entity entity)
    {
        entity.Respawn = false;
        if (entity.Definition.Flags.Solid() && IsPositionBlockedByEntity(entity, entity.SpawnPoint))
            return;

        var newEntity = EntityManager.Create(entity.Definition, entity.SpawnPoint, 0, entity.AngleRadians, entity.ThingId, entity.Args, true);
        CreateTeleportFog(entity.Position);
        CreateTeleportFog(entity.SpawnPoint);

        newEntity.Flags.SetFriendly(entity.Flags.Friendly());
        newEntity.AngleRadians = entity.AngleRadians;
        newEntity.ReactionTime = 18;

        entity.Dispose();
    }

    public bool IsPositionBlockedByEntity(Entity entity, in Vec3D position)
    {
        if (!entity.Definition.Flags.Solid())
            return true;

        double oldHeight = entity.Height;
        entity.Flags.SetSolid();
        entity.Height = entity.Definition.Properties.Height;

        // This is original functionality, the original game only checked against other things
        // It didn't check if it would clip into map geometry
        bool blocked = !BlockmapTraverser.SolidBlockTraverse(entity, entity.Position, !WorldStatic.InfinitelyTallThings);

        entity.Flags.ClearSolid();
        entity.Height = oldHeight;
        return blocked;
    }

    public bool IsPositionBlocked(Entity entity)
    {
        bool blocked = !BlockmapTraverser.SolidBlockTraverse(entity, entity.Position, !WorldStatic.InfinitelyTallThings);
        if (blocked)
            return true;

        if (!PhysicsManager.IsPositionValid(entity, entity.Position.X, entity.Position.Y))
            return true;

        return false;
    }

    public void ResetGametick() => Gametick = 0;

    public void FindKeys()
    {
        HighlightAreas.Clear();
        m_findObjects.Clear();
        MarkSpecials.FindKeys(this, m_findObjects);
        if (m_findObjects.Count == 0)
        {
            DisplayMessage("No keys found");
            return;
        }

        for (int i = 0; i < m_findObjects.Count; i++)
            HighlightAreas.Add(new HighlightArea(((Entity)m_findObjects[i]).Position, HighlightSize));
    }

    public void FindKeyLines(FindKeyLineOptions options)
    {
        m_findObjects.Clear();
        HighlightAreas.Clear();
        MarkSpecials.FindKeyLines(this, m_findObjects, options);
        if (m_findObjects.Count == 0)
        {
            DisplayMessage("No keys found");
            return;
        }

        for (int i = 0; i < m_findObjects.Count; i++)
            HighlightLine((Line)m_findObjects[i]);
    }

    public void FindExits()
    {
        m_findObjects.Clear();
        HighlightAreas.Clear();
        MarkSpecials.FindExits(this, m_findObjects);
        if (m_findObjects.Count == 0)
        {
            DisplayMessage("No exits found");
            return;
        }

        foreach (var obj in m_findObjects)
        {
            if (obj is Line line)
                HighlightLine(line);
            else if (obj is Sector sector)
                HighlightSector(sector);
        }
    }

    public bool SetSkillLevel(SkillLevel skill)
    {
        var skillDef = ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkill(skill);
        if (skillDef == null)
            return false;

        SkillLevel = skill;
        SkillDefinition = skillDef;
        return true;
    }

    private void HighlightSector(Sector sector)
    {
        var islands = Geometry.IslandGeometry.SectorIslands[sector.Id];
        if (islands.Count == 0)
            return;
        var island = islands[0];
        var center = island.Box.TopLeft + (island.Box.Width / 2, -island.Box.Height / 2);
        var pos = center.To3D(Math.Max(sector.Floor.Z, sector.Floor.Z));
        HighlightAreas.Add(new HighlightArea(pos, HighlightSize));
    }

    private void HighlightLine(Line? line)
    {
        if (line == null)
        {
            DisplayMessage("No more results");
            return;
        }

        var pos = line.Segment.FromTime(0.5).To3D(Math.Max(line.Front.Sector.Floor.Z, line.Back?.Sector.Floor.Z ?? double.MinValue));
        HighlightAreas.Add(new HighlightArea(pos, HighlightSize));
    }

    private void ApplyExplosionDamageAndThrust(Entity source, Entity attackSource, Entity entity, double radius, int maxDamage, Thrust thrust,
        bool approxDistance2D)
    {
        var applyDamage = CalcRadiusExplosionDamage(source, entity, radius, maxDamage, thrust, approxDistance2D);

        Entity? originalOwner = source.Owner();
        source.SetOwner(attackSource);
        DamageEntity(entity, source, applyDamage, DamageType.AlwaysApply, thrust);
        source.SetOwner(originalOwner);
    }

    private static int CalcRadiusExplosionDamage(Entity source, Entity entity, double radius, int maxDamage, Thrust thrust, bool approxDistance2D)
    {
        double distance;
        if (thrust == Thrust.HorizontalAndVertical && (source.Position.Z < entity.Position.Z || source.Position.Z >= entity.Position.Z + entity.Height))
        {
            Vec3D sourcePos = source.Position;
            Vec3D targetPos = entity.Position;

            if (source.Position.Z > entity.Position.Z)
                targetPos.Z += entity.Height;

            if (approxDistance2D)
                distance = Math.Max(0.0, sourcePos.ApproximateExplosionDistance2D(targetPos) - entity.Radius);
            else
                distance = Math.Max(0.0, sourcePos.Distance(targetPos) - entity.Radius);
        }
        else
        {
            if (approxDistance2D)
                distance = Math.Max(0.0, entity.Position.ApproximateExplosionDistance2D(source.Position) - entity.Radius);
            else
                distance = Math.Max(0.0, entity.Position.Distance(source.Position) - entity.Radius);
        }

        int applyDamage = Math.Clamp((int)(radius - distance), 0, maxDamage);
        if (applyDamage <= 0)
            return 0;
        return applyDamage;
    }

    protected bool ChangeToMusic(int number)
    {
        if (!MapWarp.GetMap(number, ArchiveCollection, out MapInfoDef? mapInfoDef) || mapInfoDef == null)
            return false;

        return PlayLevelMusic(mapInfoDef.Music, null);
    }

    protected void ResetLevel(bool loadLastWorldModel)
    {
        LevelChangeType type = loadLastWorldModel ? LevelChangeType.ResetOrLoadLast : LevelChangeType.Reset;
        LevelExit?.Invoke(this, new LevelChangeEvent(type, LevelChangeFlags.None));
    }

    protected virtual void PerformDispose()
    {
        foreach (var sector in Sectors)
            sector.UnlinkFromWorld(this);

        IsDisposed = true;
        UnRegisterConfigChanges();
        SpecialManager.Dispose();
        SoundManager.ClearSounds();
    }

    private void CreateBloodOrPulletPuff(Entity? entity, Vec3D intersect, double angle, double attackDistance, int damage, bool ripper = false)
    {
        if (entity != null && entity.IsDisposed)
            return;

        bool bulletPuff = entity == null || entity.Definition.Flags.NoBlood() || entity.Flags.Dormant();
        EntityDefinition? def;
        if (bulletPuff)
        {
            def = EntityManager.DefinitionComposer.BulletPuffDefinition;
            intersect.Z += Random.NextDiff() * Constants.PuffRandZ;
        }
        else
        {
            def = entity!.GetBloodDefinition();
        }

        if (def == null)
            return;

        var create = EntityManager.Create(def, intersect, 0, angle, 0, default);
        if (bulletPuff)
        {
            create.Velocity.Z = 1;
            if (create.Flags.Randomize())
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
        blood.SetOwner(entity);
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

        // Doom had the blood states hardcoded. Supercharged bulletride seems to function differing in gz vs dsda.
        // The changed the frame for blood that dsda will ignore because of hardcoded states, but work in gz.
        if (HasDehacked && entity != null && entity.FrameState.Frame.VanillaIndex != (int)ThingState.BLOOD1)
            return;

        int offset = 0;
        if (damage <= 12 && damage >= 9)
            offset = 1;
        else if (damage < 9)
            offset = 2;

        if (offset == 0)
            blood.SetRandomizeTicks();
        else if (blood.Definition.SpawnState != null)
            blood.FrameState.SetFrameIndex(blood, blood.Definition.SpawnState.Value + offset);
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

        var shootNormal = shooter.IsNormalByContext(start.Z, SolidContext.HitScan);

        for (int i = 0; i < iterateTracers; i++)
        {
            var end = (start + Vec3D.UnitSphere(setAngle, 0) * distance);
            Seg2D seg = new(start.XY, end.XY);
            var intersections = WorldStatic.Intersections;
            intersections.Clear();
            BlockmapTraverser.ShootTraverse(seg, intersections);

            double max = MaxPitch;
            double min = MinPitch;
            var status = GetBlockmapTraversalPitch(intersections, start, shooter, distance, shootNormal, SolidContext.HitScan, ref max, ref min, out pitch, out entity, out _);
            if (status == TraversalPitchStatus.PitchSet)
                return true;

            setAngle += spread;
            if (i == tracers / 2)
                setAngle = angle - tracerSpread;
        }

        return false;
    }

    private void AddSlopeSpan(double topSlope, double bottomSlope)
    {
        for (int i = 0; i < m_visibleSpans.Length; i++)
        {
            ref var span = ref m_visibleSpans.Data[i];

            // No overlap
            if (topSlope <= span.Bottom || bottomSlope >= span.Top)
                continue;

            // Fully blocked
            if (bottomSlope <= span.Bottom && topSlope >= span.Top)
            {
                m_visibleSpans.RemoveAt(i);
                i--;
                continue;
            }

            // Split span into two
            if (bottomSlope > span.Bottom && topSlope < span.Top)
            {
                if (m_visibleSpans.Length < m_visibleSpans.Capacity)
                {
                    ref var addSpan = ref m_visibleSpans.Data[m_visibleSpans.Length++];
                    addSpan.Bottom = topSlope;
                    addSpan.Top = span.Top;
                }

                span.Top = bottomSlope;
                continue;
            }

            // Cut top
            if (bottomSlope <= span.Top && bottomSlope > span.Bottom)
                span.Top = bottomSlope;

            // Cut bottom
            if (topSlope >= span.Bottom && topSlope < span.Top)
                span.Bottom = topSlope;
        }
    }

    private TraversalPitchStatus GetBlockmapTraversalPitch(DynamicArray<BlockmapIntersect> intersections, in Vec3D start, Entity startEntity, double segLength,
        bool normalSolid, SolidContext context,
        ref double topSlope, ref double bottomSlope, out double pitch, out Entity? entity, out bool crossedLine)
    {
        pitch = 0.0;
        entity = null;
        crossedLine = false;

        var data = intersections.Data;
        int length = intersections.Length;

        WorldStatic.CheckCounter++;
        m_visibleSpans.Length = 1;
        m_lastSector3D = startEntity.Sector;
        m_pitchOnBlockLine = -1;
        ref var startSpan = ref m_visibleSpans.Data[0];
        startSpan.Top = topSlope;
        startSpan.Bottom = bottomSlope;

        for (int i = 0; i < length; i++)
        {
            ref var bi = ref data[i];

            if (bi.GetIndex(out int index) == IntersectType.Line)
            {
                ref var line = ref Blockmap.BlockLines[index];
                if (line.BackSector == null || line.BlockFlags.Everything)
                    return TraversalPitchStatus.Blocked;

                if (line.FrontSector == line.BackSector)
                    continue;

                crossedLine = true;
                var segTimeLength = bi.SegTime * segLength;

                if (bi.SegTime == 0)
                {
                    m_pitchOnBlockLine = index;
                    segTimeLength = 1;
                }

                if (WorldStatic.Sector3D)
                {
                    GetOrderedSectors(line, start, out var front, out var back);

                    if (!CheckSlope3D(front, start, segTimeLength, normalSolid, context))
                        return TraversalPitchStatus.Blocked;

                    m_lastSector3D = front;

                    if (!CheckSlope3D(back, start, segTimeLength, normalSolid, context))
                        return TraversalPitchStatus.Blocked;

                    m_lastSector3D = back;
                }

                if (line.BackSector != null &&
                    line.FrontSector.Floor.Z == line.BackSector.Floor.Z &&
                    line.FrontSector.Ceiling.Z == line.BackSector.Ceiling.Z)
                    continue;

                var opening = PhysicsManager.GetLineOpening(line.FrontSector, line.BackSector!);
                if (opening.FloorZ < opening.CeilingZ)
                {
                    var floorSlope = (opening.FloorZ - start.Z) / segTimeLength;
                    var updateFloor = floorSlope > bottomSlope;
                    if (updateFloor)
                        bottomSlope = floorSlope;

                    var ceilingSlope = (opening.CeilingZ - start.Z) / segTimeLength;
                    var updateCeiling = ceilingSlope < topSlope;
                    if (updateCeiling)
                        topSlope = ceilingSlope;

                    if (WorldStatic.Sector3D && (updateFloor || updateCeiling) && !UpdateVisibleSpans(topSlope, bottomSlope))
                        return TraversalPitchStatus.Blocked;

                    if (topSlope <= bottomSlope)
                        return TraversalPitchStatus.Blocked;
                }
                else
                {
                    return TraversalPitchStatus.Blocked;
                }
            }
            else if (startEntity.Index != index)
            {
                if (bi.SegTime == 0)
                    continue;

                var segTimeLength = bi.SegTime * segLength;

                // If we didn't complete the last slope block range create one here
                if (WorldStatic.Sector3D && m_lastSector3D != null && m_lastSector3D.Sectors3D.Length > 0 &&
                    !CheckSlope3D(m_lastSector3D, start, segTimeLength, normalSolid, context))
                {
                    return TraversalPitchStatus.Blocked;
                }

                var currentEntity = DataCache.Entities[index];
                var thingTopSlope = (currentEntity.Position.Z + currentEntity.Height - start.Z) / segTimeLength;
                if (thingTopSlope < bottomSlope)
                    continue;

                var thingBottomSlope = (currentEntity.Position.Z - start.Z) / segTimeLength;
                if (thingBottomSlope > topSlope)
                    continue;

                if (thingBottomSlope > topSlope)
                    return TraversalPitchStatus.Blocked;
                if (thingTopSlope < bottomSlope)
                    return TraversalPitchStatus.Blocked;

                if (thingTopSlope < topSlope)
                    topSlope = thingTopSlope;
                if (thingBottomSlope > bottomSlope)
                    bottomSlope = thingBottomSlope;

                if (WorldStatic.Sector3D && !SetValidClipSpan(ref topSlope, ref bottomSlope))
                    return TraversalPitchStatus.Blocked;

                pitch = Math.Atan((bottomSlope + topSlope) / 2.0);
                entity = currentEntity;
                return TraversalPitchStatus.PitchSet;
            }
        }

        // If we didn't complete the last slope block range create one here
        if (WorldStatic.Sector3D && m_lastSector3D != null && m_lastSector3D.Sectors3D.Length > 0 &&
            !CheckSlope3D(m_lastSector3D, start, segLength, normalSolid, context))
        {
            return TraversalPitchStatus.Blocked;
        }

        return TraversalPitchStatus.PitchNotSet;
    }

    private bool UpdateVisibleSpans(double ceilingSlope, double floorSlope)
    {
        for (int i = 0; i < m_visibleSpans.Length; i++)
        {
            ref var span = ref m_visibleSpans.Data[i];
            if (span.Top <= floorSlope || span.Bottom >= ceilingSlope)
            {
                m_visibleSpans.RemoveAt(i);
                i--;
                continue;
            }

            if (span.Bottom < floorSlope)
                span.Bottom = floorSlope;

            if (span.Top > ceilingSlope)
                span.Top = ceilingSlope;
        }

        return m_visibleSpans.Length > 0;
    }

    private bool SetValidClipSpan(ref double thingTopSlope, ref double thingBottomSlope)
    {
        for (int i = 0; i < m_visibleSpans.Count; i++)
        {
            ref var span = ref m_visibleSpans.Data[i];
            if (thingBottomSlope < span.Top && span.Bottom < thingTopSlope)
            {
                if (span.Top < thingTopSlope)
                    thingTopSlope = span.Top;
                if (span.Bottom > thingBottomSlope)
                    thingBottomSlope = span.Bottom;

                if (thingTopSlope <= thingBottomSlope)
                    continue;

                return true;
            }
        }

        return false;
    }

    private bool CheckSlope3D(Sector sector, in Vec3D start, double segTimeLength, bool normalSolid, SolidContext context)
    {
        for (int i = 0; i < sector.Sectors3D.Length; i++)
        {
            var sector3D = sector.Sectors3D[i];
            // Only flip for HitScan. GZDoom forces LOS to block here. Non-solid inverted sectors that touch will block visibility.
            if (!normalSolid && context == SolidContext.HitScan)
            {
                if (!sector3D.IsInvertedByContext(context))
                    continue;
            }
            else
            {
                if (!sector3D.IsSolidByContext(context))
                    continue;
            }

            var topZ = sector3D.ControlTop.Z;
            var bottomZ = sector3D.ControlBottom.Z;
            var topSlope3D = (topZ - start.Z) / segTimeLength;
            var bottomSlope3D = (bottomZ - start.Z) / segTimeLength;

            // If leaving the current 3D sector then add this interval and create a new one from the start point to complete the span.
            if (m_lastSector3D == sector)
            {
                AddSlopeSpan(topSlope3D, bottomSlope3D);
                if (m_visibleSpans.Length == 0)
                    return false;

                // If started in a 3D sector then the slope needs be initialized.
                // Since the seg length is zero set to the z distance.
                if (sector3D.LastSlopeCheckCount != WorldStatic.CheckCounter)
                {
                    sector3D.LastSlopeTop = topZ - start.Z;
                    sector3D.LastSlopeBottom = bottomZ - start.Z;
                }

                if (start.Z < bottomZ)
                {
                    bottomSlope3D = topSlope3D;
                    topSlope3D = sector3D.LastSlopeTop;
                }
                else
                {
                    topSlope3D = bottomSlope3D;
                    bottomSlope3D = sector3D.LastSlopeBottom;
                }
            }
            else
            {
                sector3D.LastSlopeCheckCount = WorldStatic.CheckCounter;
                sector3D.LastSlopeTop = topSlope3D;
                sector3D.LastSlopeBottom = bottomSlope3D;
            }

            AddSlopeSpan(topSlope3D, bottomSlope3D);
            if (m_visibleSpans.Length == 0)
                return false;
        }

        return true;
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
        if (intersect.Z <= floorZ)
        {
            sector.Floor.Plane.Intersects(start, end, ref intersect);
            intersect.Z = sector.ToFloorZ(intersect);
        }
        else if (intersect.Z >= ceilingZ)
        {
            sector.Ceiling.Plane.Intersects(start, end, ref intersect);
            intersect.Z = sector.ToCeilingZ(intersect) - 4;
        }
    }

    private static void GetOrderedSectors(in BlockLine line, in Vec3D start, out Sector front, out Sector back)
    {
        // On front of line
        if (line.Segment.PerpDot(start) <= 0)
        {
            front = line.FrontSector;
            back = line.BackSector!;
        }
        else
        {
            front = line.BackSector!;
            back = line.FrontSector;
        }
    }

    public void CreateTeleportFog(in Vec3D pos)
    {
        if (m_teleportFogDef == null)
            return;

        var teleport = EntityManager.Create(m_teleportFogDef, pos, 0.0, 0.0, 0, default);
        SoundManager.CreateSoundOn(teleport, Constants.TeleportSound, new SoundParams(teleport));
    }

    public void CreateTeleportFog(Entity entity)
    {
        if (m_teleportFogDef == null)
            return;

        var fogDist = Vec2D.UnitCircle(entity.AngleRadians) * Constants.TeleportOffsetDist;
        var teleportFogPos = entity.Position;
        teleportFogPos.X += fogDist.X;
        teleportFogPos.Y += fogDist.Y;

        CreateTeleportFog(teleportFogPos);
    }

    public Entity? SpawnEntity(EntityDefinition definition, in Vec3D pos, int tid, double angle, in SpecialArgs args, bool teleportFog)
    {
        if (!BlockmapTraverser.SolidBlockTraverse(definition, pos, !WorldStatic.InfinitelyTallThings))
            return null;

        var entity = EntityManager.Create(definition, pos, 0, angle, tid, args);
        if (teleportFog && entity != null)
            CreateTeleportFog(entity.Position);

        return entity;
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
                LevelExit?.Invoke(this, new LevelChangeEvent(levelCheat.LevelNumber, isCheat: true));
                return;
            }
            else if (levelCheat.CheatType == CheatType.ChangeMusic && !ChangeToMusic(levelCheat.LevelNumber))
            {
                return;
            }
        }

        bool isActive = player.Cheats.IsCheatActive(cheat.CheatType);
        switch (cheat.CheatType)
        {
            case CheatType.NoClip:
                player.Flags.SetNoClip(isActive);
                break;
            case CheatType.Fly:
                player.Flags.SetFly(isActive);
                player.Flags.SetNoGravity(isActive);
                break;
            case CheatType.Kill:
                ClearConsole?.Invoke(this, EventArgs.Empty);
                player.ForceGib();
                break;
            case CheatType.Resurrect:
                ClearConsole?.Invoke(this, EventArgs.Empty);
                if (player.IsDead())
                    player.SetRaiseState();
                break;
            case CheatType.KillAllMonsters:
                ClearConsole?.Invoke(this, EventArgs.Empty);
                DisplayMessage(player, null, $"{KillAllMonsters(0)} {ArchiveCollection.Language.GetMessage(cheat.CheatOn)}");
                break;
            case CheatType.God:
                if (!player.IsDead())
                    SetGodModeHealth(player);
                player.Flags.SetInvulnerable(isActive);
                break;
            case CheatType.GiveAllNoKeys:
                player.GiveAllWeapons(EntityManager.DefinitionComposer);
                GiveCheatArmor(player, cheat.CheatType);
                break;
            case CheatType.GiveAll:
                player.GiveAllWeapons(EntityManager.DefinitionComposer);
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
            case CheatType.Exit:
            case CheatType.ExitSecret:
                ClearConsole?.Invoke(this, EventArgs.Empty);
                ExitLevel(cheat.CheatType == CheatType.ExitSecret ? ExitLevelArgs.NextSecretMap() : ExitLevelArgs.NextMap());
                break;
            case CheatType.EndGame:
                ClearConsole?.Invoke(this, EventArgs.Empty);
                ExitLevel(ExitLevelArgs.EndGame());
                break;
        }
    }

    public int KillAllMonsters(int sectorTag)
    {
        int killCount = 0;
        for (var entity = EntityManager.Head; entity != null; entity = entity.Next)
        {
            if (sectorTag != 0 && entity.Sector.Tag != sectorTag)
                continue;

            if (!entity.IsDead() && (entity.Flags.CountKill() || entity.Flags.IsMonster()))
            {
                entity.ForceGib();
                killCount++;
            }
        }

        return killCount;
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

    public int EntityCount(int entityDefinitionId) =>
        EntityCount(entityDefinitionId, false);

    public int EntityAliveCount(int entityDefinitionId, Entity? ignoreEntity = null) =>
        EntityCount(entityDefinitionId, true, ignoreEntity);

    private int EntityCount(int entityDefinitionId, bool checkAlive, Entity? ignoreEntity = null)
    {
        int count = 0;
        for (var entity = EntityManager.Head; entity != null; entity = entity.Next)
        {
            if (entity == ignoreEntity)
                continue;

            if (entity.Definition.Id == entityDefinitionId && (!checkAlive || !entity.IsDead()))
                count++;
        }
        return count;
    }

    public bool HealChase(Entity entity, EntityFrame healState, string healSound)
    {
        m_healChaseData.HealEntity = entity;
        m_healChaseData.HealState = healState;
        m_healChaseData.HealSound = healSound;
        m_healChaseData.Healed = false;
        var moveFactor = PhysicsManager.GetMoveFactor(entity);
        entity.GetEnemySpeed(moveFactor, out var speedX, out var speedY);
        Box2D nextBox = new(entity.Position.X + speedX, entity.Position.Y + speedY, entity.Radius);
        BlockmapTraverser.HealTraverse(nextBox, m_healChaseAction);

        return m_healChaseData.Healed;
    }

    private void HandleHealChase(Entity entity)
    {
        var healChaseEntity = m_healChaseData.HealEntity;
        m_healChaseData.Healed = true;
        entity.Flags.SetSolid();
        entity.Height = entity.Definition.Properties.Height;

        var saveTarget = healChaseEntity.Target();
        healChaseEntity.SetTarget(entity);
        EntityActionFunctions.A_FaceTarget(healChaseEntity);
        healChaseEntity.SetTarget(saveTarget);
        healChaseEntity.FrameState.SetState(entity, m_healChaseData.HealState);

        if (m_healChaseData.HealSound.Length > 0)
            WorldStatic.SoundManager.CreateSoundOn(entity, m_healChaseData.HealSound, new SoundParams(entity));

        bool setVileGhost = Config.Compatibility.VileGhosts && entity.Flags.CrushGiblets();
        entity.SetRaiseState(!setVileGhost);
        if (setVileGhost)
        {
            entity.Flags.SetShootable(entity.Definition.Flags.Shootable());
            entity.Flags.ClearSolid();
            entity.Height = 0;
            entity.Radius = 0;
        }
        entity.Flags.SetFriendly(healChaseEntity.Flags.Friendly());
    }

    public void TracerSeek(Entity entity, double threshold, double maxTurnAngle, GetTracerVelocityZ velocityZ)
    {
        var tracer = entity.Tracer();
        if (tracer == null)
            return;

        if (tracer.IsDead() || !tracer.Flags.Shootable())
        {
            entity.SetTracer(null);
            return;
        }

        SetTracerAngle(entity, threshold, maxTurnAngle);

        double z = entity.Velocity.Z;
        entity.Velocity = Vec3D.UnitSphere(entity.AngleRadians, 0.0) * entity.Definition.Properties.MissileMovementSpeed;
        entity.Velocity.Z = z;

        entity.Velocity.Z = velocityZ(entity, tracer);
    }

    public void SetNewTracerTarget(Entity entity, double fieldOfViewRadians, double radius)
    {
        m_newTracerTargetData.Entity = entity;
        m_newTracerTargetData.Owner = entity.Owner() ?? entity;
        m_newTracerTargetData.FieldOfViewRadians = fieldOfViewRadians;
        m_newTracerTargetData.TargetEntity = null;
        BlockmapTraverser.EntityTraverseSpiralBlocks(entity.Position.X, entity.Position.Y, radius, m_setNewTracerTargetAction);

        if (m_newTracerTargetData.TargetEntity != null)
            entity.SetTracer(m_newTracerTargetData.TargetEntity);
    }

    private GridIterationStatus HandleSetNewTracerTarget(Entity checkEntity)
    {
        if (!checkEntity.Flags.Shootable())
            return GridIterationStatus.Continue;

        if (m_newTracerTargetData.Owner == checkEntity || !m_newTracerTargetData.Owner.ValidEnemyTarget(checkEntity))
            return GridIterationStatus.Continue;

        if (m_newTracerTargetData.FieldOfViewRadians > 0 &&
            !InFieldOfView(m_newTracerTargetData.Entity, checkEntity, m_newTracerTargetData.FieldOfViewRadians))
            return GridIterationStatus.Continue;

        if (!CheckLineOfSight(m_newTracerTargetData.Entity, checkEntity))
            return GridIterationStatus.Continue;

        m_newTracerTargetData.TargetEntity = checkEntity;
        return GridIterationStatus.Stop;
    }

    public void EntityTeleported(Entity teleportEntity)
    {
        teleportEntity.ClearMonsterCloset();

        if (teleportEntity.PlayerObj == null || teleportEntity.PlayerObj.IsVooDooDoll)
            return;

        var playerSubsectorId = teleportEntity.SubsectorId;
        if (playerSubsectorId < 0 || playerSubsectorId >= Geometry.SubsectorToIslandId.Length)
            return;

        var playerIslandId = Geometry.SubsectorToIslandId[playerSubsectorId];
        if (playerIslandId < 0 || playerIslandId >= Geometry.IslandGeometry.Islands.Count)
            return;

        var island = Geometry.IslandGeometry.Islands[playerIslandId];
        bool wasMonsterCloset = island.IsMonsterCloset;
        bool wasVooDooCloset = island.IsVooDooCloset;

        if (wasMonsterCloset || wasVooDooCloset)
            ClearSectorIslandClosetStatus(island, wasMonsterCloset, wasVooDooCloset);

        island.IsMonsterCloset = false;
        island.IsVooDooCloset = false;

        if (!wasMonsterCloset)
            return;

        // Whoops. Player teleported into a monster closet.       
        for (var entity = EntityManager.Head; entity != null; entity = entity.Next)
        {
            if ((entity.ClosetFlags & ClosetFlags.MonsterCloset) != 0)
                continue;

            if (entity.SubsectorId < 0 || entity.SubsectorId >= Geometry.SubsectorToIslandId.Length)
                continue;

            var islandId = Geometry.SubsectorToIslandId[entity.SubsectorId];
            if (islandId < 0 || islandId >= Geometry.IslandGeometry.Islands.Count)
                continue;

            if (islandId != island.Id)
                continue;

            entity.ClearMonsterCloset();
        }
    }

    private static void ClearSectorIslandClosetStatus(Island island, bool wasMonsterCloset, bool wasVooDooCloset)
    {
        for (int i = 0; i < WorldStatic.World.Geometry.IslandGeometry.SectorIslands.Length; i++)
        {
            var sectorIslands = WorldStatic.World.Geometry.IslandGeometry.SectorIslands[i];
            for (int j = 0; j < sectorIslands.Count; j++)
            {
                var sectorIsland = sectorIslands[j];
                if (sectorIsland.ParentIsland != island)
                    continue;

                if (wasVooDooCloset)
                    sectorIsland.IsVooDooCloset = false;
                if (wasMonsterCloset)
                    sectorIsland.IsMonsterCloset = false;
            }
        }
    }

    public void SetEntityPosition(Entity entity, Vec3D pos)
    {
        entity.ResetInterpolation();
        entity.UnlinkFromWorld();
        entity.Position = pos;
        Link(entity);
    }

    private static void SetTracerAngle(Entity entity, double threshold, double maxTurnAngle)
    {
        var tracer = entity.Tracer();
        if (tracer == null)
            return;
        // Doom's angles were always 0-360 and did not allow negatives (thank you arithmetic overflow)
        // To keep this code familiar GetPositiveAngle will keep angle between 0 and 2pi
        double exact = MathHelper.GetPositiveAngle(entity.Position.Angle(tracer.Position));
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

            if (armor.Count != 0)
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

        if (powerupType == PowerupType.ComputerAreaMap)
        {
            // Not really a powerup, part of inventory
            if (player.Inventory.HasItem(def.Name))
                player.Inventory.Remove(def.Name, 1);
            else
                player.Inventory.Add(def, 1);
        }
        else if (powerupType == PowerupType.Strength)
        {
            // Triggered from item pickup state
            var berserk = EntityManager.Create("Berserk", Vec3D.Zero);
            if (berserk != null)
                PerformItemPickup(player, berserk);
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
        }

        return PowerupType.None;
    }

    private void GiveChainsaw(Player player)
    {
        var chainsaw = EntityManager.DefinitionComposer.GetByName("chainsaw");
        if (chainsaw != null)
            player.GiveWeapon(chainsaw);
    }

    private void ApplyVooDooDamage(Player player, int damage, bool setPainState)
    {
        if (!player.IsVooDooDoll || EntityManager.VoodooDolls.Count == 0 || player.IsSyncVooDoo)
            return;

        SyncVooDooDollWithPlayer(player);
        Player? updatePlayer = EntityManager.GetRealPlayer(player.PlayerNumber);
        if (updatePlayer == null)
            return;

        updatePlayer.Damage(null, damage, setPainState, DamageType.AlwaysApply);
        CompleteVooDooDollSync();
    }

    private void ApplyVooDooKill(Player player, Entity? source, bool forceGib)
    {
        if (EntityManager.VoodooDolls.Count == 0 || player.IsSyncVooDoo)
            return;

        SyncVooDooDollWithPlayer(player);
        Player? updatePlayer = EntityManager.GetRealPlayer(player.PlayerNumber);
        if (updatePlayer == null)
            return;

        if (forceGib)
        {
            updatePlayer.ForceGib();
            player.ForceGib();
        }
        else
        {
            updatePlayer.Kill(source);
            player.Kill(source);
        }

        CompleteVooDooDollSync();
    }

    private bool GiveVooDooItem(Player player, EntityDefinition pickupDef, EntityFlags? flags, bool pickupFlash)
    {
        Player? updatePlayer = EntityManager.GetRealPlayer(player.PlayerNumber);
        if (updatePlayer == null)
            return false;

        bool success = updatePlayer.GiveItem(pickupDef, flags, pickupFlash);
        if (!success)
            return false;

        return true;
    }

    private void SyncVooDooDollWithPlayer(Player voodooDoll)
    {
        Player? realPlayer = EntityManager.GetRealPlayer(voodooDoll.PlayerNumber);
        if (realPlayer == null)
            return;

        for (int i = 0; i < EntityManager.Players.Count; i++)
            EntityManager.Players[i].IsSyncVooDoo = true;

        voodooDoll.IsSyncVooDoo = true;
        voodooDoll.VoodooSync(realPlayer);
    }

    private void CompleteVooDooDollSync()
    {
        for (int i = 0; i < EntityManager.Players.Count; i++)
            EntityManager.Players[i].IsSyncVooDoo = false;

        for (int i = 0; i < EntityManager.VoodooDolls.Count; i++)
            EntityManager.VoodooDolls[i].IsSyncVooDoo = false;
    }

    public void SetSideTexture(Side side, WallLocation location, int textureHandle)
    {
        int previousTextureHandle;
        Wall wall;
        switch (location)
        {
            case WallLocation.Upper:
                previousTextureHandle = side.Upper.TextureHandle;
                wall = side.Upper;
                break;
            case WallLocation.Lower:
                previousTextureHandle = side.Lower.TextureHandle;
                wall = side.Lower;
                break;
            case WallLocation.Middle:
            default:
                previousTextureHandle = side.Middle.TextureHandle;
                wall = side.Middle;
                break;
        }

        side.SetWallTexture(textureHandle, location);
        SideTextureChanged?.Invoke(this, new SideTextureEvent(side, wall, textureHandle, previousTextureHandle));
    }

    public void SetPlaneTexture(SectorPlane plane, int textureHandle)
    {
        int previousTextureHandle = plane.TextureHandle;
        if (textureHandle == previousTextureHandle)
            return;

        plane.SetTexture(textureHandle, Gametick);
        PlaneTextureChanged?.Invoke(this, new PlaneTextureEvent(plane, textureHandle, previousTextureHandle));
    }

    public void SetSectorLightLevel(Sector sector, short lightLevel)
    {
        sector.SetLightLevel(lightLevel, Gametick);
        SectorLightChanged?.Invoke(this, sector);
    }

    public void SetSectorFloorLightLevel(Sector sector, short lightLevel)
    {
        sector.SetFloorLightLevel(lightLevel, Gametick);
        SectorLightChanged?.Invoke(this, sector);
    }

    public void SetSectorCeilingLightLevel(Sector sector, short lightLevel)
    {
        sector.SetCeilingLightLevel(lightLevel, Gametick);
        SectorLightChanged?.Invoke(this, sector);
    }

    public void SetSectorEffect(Sector sector, SectorEffect effect)
    {
        sector.SetSectorEffect(effect);
    }

    public void SetSectorKillEffect(Sector sector, InstantKillEffect effect)
    {
        sector.SetKillEffect(effect);
    }

    public void SetSectorColorMap(Sector sector, Colormap? colormap)
    {
        if (sector.Colormap == colormap)
            return;
        sector.SetColorMap(colormap);
        SectorColorMapChanged?.Invoke(this, sector);
    }

    private bool EntityActivatedSpecial(in EntityActivateSpecial args) =>
        SpecialManager.TryAddActivatedLineSpecial(args);

    public virtual void ToggleChaseCameraMode()
    {
    }

    public virtual Player GetCameraPlayer() => Player;

    public bool GetPickupPlayer(Entity entity, [NotNullWhen(true)] out Player? player) => 
        m_itemPickupIndexToPlayers.TryGetValue(entity.Index, out player);

    public bool ShouldSpawn(IThing mapThing)
    {
        var flags = mapThing.Flags;
        if (WorldType == WorldType.SinglePlayer)
        {
            return m_spawnMulti switch
            {
                SpawnMulti.SinglePlayerAndCoop => ShouldSpawn(flags, MapType, SpawnFilter.SinglePlayer) || ShouldSpawn(flags, MapType, SpawnFilter.Cooperative),
                SpawnMulti.CoopOnly => ShouldSpawn(flags, MapType, SpawnFilter.Cooperative),
                _ => ShouldSpawn(flags, MapType, SpawnFilter.SinglePlayer),
            };
        }

        var filter = SpawnFilter.None;
        if (WorldType == WorldType.Deathmatch)
            filter = SpawnFilter.Deathmatch;
        else if (WorldType == WorldType.Cooperative)
            filter = SpawnFilter.Cooperative;

        return ShouldSpawn(flags, MapType, filter);
    }

    private static bool ShouldSpawn(ThingFlags flags, MapType mapType, SpawnFilter filter)
    {
        if (filter == SpawnFilter.SinglePlayer)
        {
            if (mapType != MapType.Doom)
                return flags.SinglePlayer;

            return !flags.MultiPlayer;
        }

        if (mapType == MapType.Doom)
        {
            if (filter == SpawnFilter.Cooperative)
                return !flags.NotCooperative;
            if (filter == SpawnFilter.Deathmatch)
                return !flags.NotDeathmatch;
        }

        if (filter == SpawnFilter.Cooperative)
            return flags.Cooperative;
        if (filter == SpawnFilter.Deathmatch)
            return flags.Deathmatch;

        return true;
    }

    public virtual Player? RespawnPlayer(Player player)
    {
        var spawn = EntityManager.SpawnLocations.GetPlayerSpawn(player.PlayerNumber);
        if (spawn == null)
            return null;

        var stats = player.PlayerStats;
        player.PlayerState = PlayerState.Ignore;
        player = EntityManager.RespawnPlayer(0, spawn);
        player.PlayerStats = stats;
        player.SetDefaultInventory();

        CreateTeleportFog(player);
        return player;
    }

    public Entity? Summon(Entity source, EntityDefinition definition, SummonOptions options)
    {
        if (definition.Flags.Missile() && options != SummonOptions.Static)
        {
            var pitch = 0.0;
            if (source.PlayerObj != null)
                pitch = source.PlayerObj.PitchRadians;

            return FireProjectile(Player, source.AngleRadians, pitch, Constants.EntityShootDistance,
                Config.Game.AutoAim, definition, out _);
        }

        var unit = Vec2D.UnitCircle(source.AngleRadians);
        var pos2D = source.Position.XY + unit * (source.Radius + definition.Properties.Radius + 40);
        var pos = pos2D.To3D(ToSubsector(pos2D.X, pos2D.Y).Sector.Floor.Z);

        if (definition.Flags.Solid() && !BlockmapTraverser.SolidBlockTraverse(definition, pos, !WorldStatic.InfinitelyTallThings))
            return null;

        var entity = EntityManager.Create(definition.Name, pos);
        if (entity != null)
        {
            entity.AngleRadians = source.AngleRadians;
            switch (options)
            {
                case SummonOptions.Friend:
                    entity.Flags.SetFriendly();
                    break;
                case SummonOptions.Foe:
                    entity.Flags.ClearFriendly();
                    break;
                case SummonOptions.Static:
                    entity.Position.Z = source.ProjectileAttackPos.Z;
                    entity.PrevPosition.Z = source.ProjectileAttackPos.Z;
                    break;
            }
        }

        return entity;
    }
}
