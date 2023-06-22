using System;
using Helion.Audio;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Maps;
using Helion.Models;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Values;
using Helion.Util.Consoles;
using Helion.Util.Profiling;
using Helion.Util.RandomGenerators;
using Helion.Util.Timing;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Geometry.Builder;
using Helion.World.Impl.SinglePlayer;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Layer.Worlds;

public partial class WorldLayer : IGameLayerParent
{
    private const int TickOverflowThreshold = (int)(10 * Constants.TicksPerSecond);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public IntermissionLayer? Intermission { get; private set; }
    public MapInfoDef CurrentMap { get; }
    public SinglePlayerWorld World { get; }
    private readonly IConfig m_config;
    private readonly HelionConsole m_console;
    private readonly GameLayerManager m_parent;
    private readonly Ticker m_ticker = new(Constants.TicksPerSecond);
    private readonly FpsTracker m_fpsTracker;
    private readonly Profiler m_profiler;
    private readonly TickCommand m_tickCommand = new();
    private readonly TickCommand m_chaseCamTickCommand = new();
    private readonly TickCommand m_demoTickCommand = new();
    private TickerInfo m_lastTickInfo = new(0, 0);
    private bool m_drawAutomap;
    private Vec2I m_autoMapOffset = (0, 0);
    private double m_autoMapScale;
    private bool m_disposed;
    private bool m_paused;

    private Player Player => World.Player;
    public bool ShouldFocus => !World.Paused || (World.IsChaseCamMode && !AnyLayerObscuring);

    public WorldLayer(GameLayerManager parent, IConfig config, HelionConsole console, FpsTracker fpsTracker, 
        SinglePlayerWorld world, MapInfoDef mapInfoDef, Profiler profiler)
    {
        m_worldContext = new(m_camera, 0);
        m_hudContext = new(new Dimension());
        m_config = config;
        m_console = console;
        m_parent = parent;
        m_fpsTracker = fpsTracker;
        m_autoMapScale = config.Hud.AutoMap.Scale;
        m_profiler = profiler;
        World = world;
        CurrentMap = mapInfoDef;

        m_drawAutomapAndHudAction = new(DrawAutomapAndHudContext);
        m_virtualDrawHudWeaponAction = new(VirtualDrawHudWeapon);
        m_renderWorldAction = new(RenderWorld);

        m_ticker.Start();
    }

    ~WorldLayer()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public static WorldLayer? Create(GameLayerManager parent, GlobalData globalData, IConfig config,
        HelionConsole console, IAudioSystem audioSystem, ArchiveCollection archiveCollection,
        FpsTracker fpsTracker, Profiler profiler, MapInfoDef mapInfoDef, SkillDef skillDef, IMap map,
        Player? existingPlayer, WorldModel? worldModel, IRandom? random)
    {
        string displayName = mapInfoDef.GetMapNameWithPrefix(archiveCollection);
        Log.Info(displayName);

        SinglePlayerWorld? world = CreateWorldGeometry(globalData, config, audioSystem, archiveCollection, profiler,
            mapInfoDef, skillDef, map, existingPlayer, worldModel, random);
        if (world == null)
            return null;

        ApplyConfiguration(config, archiveCollection, skillDef, worldModel);
        config.ApplyQueuedChanges(ConfigSetFlags.OnNewWorld);
        return new WorldLayer(parent, config, console, fpsTracker, world, mapInfoDef, profiler);
    }

    private static void ApplyConfiguration(IConfig config, ArchiveCollection archiveCollection, SkillDef skillDef, WorldModel? worldModel)
    {
        config.Game.Skill.Set(archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkillLevel(skillDef));

        if (worldModel == null)
            return;

        config.ApplyConfiguration(worldModel.ConfigValues);
    }

    public static SinglePlayerWorld? CreateWorldGeometry(GlobalData globalData, IConfig config, IAudioSystem audioSystem,
        ArchiveCollection archiveCollection, Profiler profiler, MapInfoDef mapDef, SkillDef skillDef, IMap map,
        Player? existingPlayer, WorldModel? worldModel, IRandom? random, bool unitTest = false)
    {
        archiveCollection.InitTextureManager(mapDef, unitTest);
        MapGeometry? geometry = GeometryBuilder.Create(map, config, archiveCollection.TextureManager);
        if (geometry == null)
            return null;

        try
        {
            return new SinglePlayerWorld(globalData, config, archiveCollection, audioSystem, profiler, geometry,
                mapDef, skillDef, map,existingPlayer, worldModel, random);
        }
        catch (HelionException e)
        {
            Log.Error(e.Message);
        }

        return null;
    }

    public void Remove(object layer)
    {
        if (ReferenceEquals(layer, Intermission))
        {
            Intermission?.Dispose();
            Intermission = null;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        PerformDispose();
    }

    private void PerformDispose()
    {
        if (m_disposed)
            return;

        World.Dispose();

        Intermission?.Dispose();
        Intermission = null;

        m_disposed = true;
    }
}
