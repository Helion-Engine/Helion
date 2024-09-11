using System;
using System.Collections.Generic;
using System.Reflection;
using Helion.Audio;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics.Fonts;
using Helion.Maps;
using Helion.Models;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.OpenGL.Renderers.Legacy.World.Automap;
using Helion.Render.OpenGL.Texture.Fonts;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Values;
using Helion.Util.Consoles;
using Helion.Util.Extensions;
using Helion.Util.Loggers;
using Helion.Util.Profiling;
using Helion.Util.RandomGenerators;
using Helion.Util.Timing;
using Helion.Window;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Geometry.Builder;
using Helion.World.Impl.SinglePlayer;
using Helion.World.StatusBar;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Layer.Worlds;

public partial class WorldLayer : IGameLayerParent
{
    private const int TickOverflowThreshold = (int)(10 * Constants.TicksPerSecond);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static string LastMapName = string.Empty;
    private static MapGeometry? LastMapGeometry;
    private static IslandGeometry LastIslandGeometry = new();

    record class MapCompatibility(string Name, string MapName, string MD5, IList<(FieldInfo, bool)> Values);

    private static readonly MapCompatibility[] MapCompat = new MapCompatibility[]
    {
        new("TNT MAP30", "MAP30", "d41d8cd98f00b204e9800998ecf8427e",
            GetConfigCompatProperties((nameof(ConfigCompat.Stairs), true)))
    };

    public IntermissionLayer? Intermission { get; private set; }
    public MapInfoDef CurrentMap { get; }
    public SinglePlayerWorld World { get; }

    public bool DrawAutomap { get; private set; }

    private readonly IConfig m_config;
    private readonly HelionConsole m_console;
    private readonly GameLayerManager m_parent;
    private readonly FpsTracker m_fpsTracker;
    private readonly Profiler m_profiler;
    private readonly TickCommand m_tickCommand = new();
    private readonly TickCommand m_chaseCamTickCommand = new();
    private readonly TickCommand m_demoTickCommand = new();
    private readonly Action<IHudRenderContext> m_virtualDrawFullStatusBarAction;
    private readonly Action<HudStatusBarbackground> m_virtualStatusBarBackgroundAction;
    private readonly Action<IHudRenderContext> m_virtualDrawPauseAction;
    private readonly Func<IConsumableInput, KeyCommandItem, bool> m_checkCommandAction;
    private StatusBarSizeType m_statusBarSizeType = StatusBarSizeType.Minimal;
    private TickerInfo m_lastTickInfo = new(0, 0);
    private Vec2I m_autoMapOffset = (0, 0);
    private double m_autoMapScale;
    private bool m_disposed;
    private bool m_paused;

    private Player Player => World.Player;
    public bool ShouldFocus => !World.Paused || (World.IsChaseCamMode && !AnyLayerObscuring);
    private readonly Font DefaultFont;

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

        m_drawHudAction = new(DrawHudContext);
        m_renderWorldAction = new(RenderWorld);
        m_renderAutomapAction = new(RenderAutomap);
        m_virtualDrawFullStatusBarAction = new(VirtualDrawFullStatusBar);
        m_virtualStatusBarBackgroundAction = new(VirtualStatusBarBackground);
        m_virtualDrawPauseAction = new(VirtualDrawPause);
        m_checkCommandAction = new(CheckCommand);

        var font = World.ArchiveCollection.GetFont(LargeHudFont);
        font ??= new Font("Empty", [], new((0, 0), Graphics.ImageType.Argb));
        DefaultFont = font;

        m_renderHealthString = InitRenderableString();
        m_renderArmorString = InitRenderableString();
        m_renderAmmoString = InitRenderableString();
        m_renderFpsString = InitRenderableString(TextAlign.Right);
        m_renderFpsMinString = InitRenderableString(TextAlign.Right);
        m_renderFpsMaxString = InitRenderableString(TextAlign.Right);
        m_renderTimeString = InitRenderableString(TextAlign.Right);
        m_renderKillString = InitRenderableString(TextAlign.Right);
        m_renderItemString = InitRenderableString(TextAlign.Right);
        m_renderSecretString = InitRenderableString(TextAlign.Right);
        m_renderKillLabel = InitRenderableString(TextAlign.Right);
        m_renderItemLabel = InitRenderableString(TextAlign.Right);
        m_renderSecretLabel = InitRenderableString(TextAlign.Right);

        StatValues = [m_killString, m_itemString, m_secretString];
        RenderableStatLabels = [m_renderKillLabel, m_renderItemLabel, m_renderSecretLabel];
        RenderableStatValues = [m_renderKillString, m_renderItemString, m_renderSecretString];
        m_largeHudFont = GetFontOrDefault(LargeHudFont);
    }

    private RenderableString InitRenderableString(TextAlign align = TextAlign.Left) => 
        new(World.ArchiveCollection.DataCache, string.Empty, DefaultFont, 12, align: align, shouldFree: false);

    private Font GetFontOrDefault(string name)
    {
        var font = World.ArchiveCollection.GetFont(name);
        if (font == null)
            return DefaultFont;
        return font;
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

        bool sameAsPreviousMap = mapInfoDef.MapName.EqualsIgnoreCase(LastMapName); if (!sameAsPreviousMap)
            HelionLog.Info(displayName);

        SinglePlayerWorld? world = CreateWorldGeometry(globalData, config, audioSystem, archiveCollection, profiler,
            mapInfoDef, skillDef, map, existingPlayer, worldModel, random, sameAsPreviousMap: sameAsPreviousMap);
        if (world == null)
            return null;

        if (!sameAsPreviousMap && archiveCollection.Definitions.CompLevelDefinition.CompLevel == CompLevel.Undefined)
            SetCompatibilityOptions(config, map, mapInfoDef, archiveCollection);

        archiveCollection.TextureManager.InitSprites(world);
        LastMapName = mapInfoDef.MapName;
        ApplyConfiguration(config, archiveCollection, skillDef, worldModel);
        config.ApplyQueuedChanges(ConfigSetFlags.OnNewWorld);
        return new WorldLayer(parent, config, console, fpsTracker, world, mapInfoDef, profiler);
    }

    private static void SetCompatibilityOptions(IConfig config, IMap map, MapInfoDef mapInfoDef, ArchiveCollection archiveCollection)
    {
        // Complevel is a global modifier. Cannot restore compatibility options here.
        if (archiveCollection.Definitions.CompLevelDefinition.CompLevel != CompLevel.Undefined)            
            return;

        var compat = config.Compatibility;
        compat.ResetToUserValues();

        if (mapInfoDef.HasOption(MapOptions.CompatMissileClip))
            compat.MissileClip.Set(true, writeToConfig: false);
        if (mapInfoDef.HasOption(MapOptions.CompatShortestTexture))
            compat.VanillaShortestTexture.Set(true, writeToConfig: false);
        if (mapInfoDef.HasOption(MapOptions.CompatFloorMove))
            compat.VanillaSectorPhysics.Set(true, writeToConfig: false);
        if (mapInfoDef.HasOption(MapOptions.CompatNoCrossOver))
            compat.InfinitelyTallThings.Set(true, writeToConfig: false);
        if (mapInfoDef.HasOption(MapOptions.CompatLimitPain))
            compat.PainElementalLostSoulLimit.Set(true, writeToConfig: false);
        if (mapInfoDef.HasOption(MapOptions.CompatNoTossDrops))
            compat.NoTossDrops.Set(true, writeToConfig: false);
        if (mapInfoDef.HasOption(MapOptions.CompatStairs))
            compat.Stairs.Set(true, writeToConfig: false);
        if (mapInfoDef.HasOption(MapOptions.CompatExplosion1) || mapInfoDef.HasOption(MapOptions.CompatExplosion2)) // These aren't separated like ZDoom, but are unlikely to be toggled independently
            compat.OriginalExplosion.Set(true, writeToConfig: false);
        if (mapInfoDef.HasOption(MapOptions.VileGhosts))
            compat.VileGhosts.Set(true, writeToConfig: false);

        foreach (var mapCompat in MapCompat)
        {
            if (map.Name.EqualsIgnoreCase(mapCompat.MapName) && map.MD5.Equals(mapCompat.MD5))
            {
                ApplyCompatOptions(config, mapCompat.Values);
                break;
            }
        }
    }

    private static void ApplyCompatOptions(IConfig config, IList<(FieldInfo, bool)> props)
    {
        foreach (var (field, set) in props)
        {
            if (field.GetValue(config.Compatibility) is not ConfigValue<bool> configValue)
                continue;
            configValue.Set(set, writeToConfig: false);
        }
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
        Player? existingPlayer, WorldModel? worldModel, IRandom? random, bool unitTest = false, bool sameAsPreviousMap = false)
    {
        archiveCollection.InitTextureManager(mapDef, unitTest);

        MapGeometry? geometry;
        if (sameAsPreviousMap && LastMapGeometry != null)
            geometry = GetGeometryAndReset(LastMapGeometry);
        else
            geometry = GeometryBuilder.Create(map, config, archiveCollection.TextureManager);

        if (geometry == null)
            return null;

        LastMapGeometry = geometry;

        if (sameAsPreviousMap)
        {
            geometry.IslandGeometry = LastIslandGeometry;
        }
        else
        {
            geometry.ClassifyIslands();
            LastIslandGeometry = geometry.IslandGeometry;
        }

        try
        {
            bool reuse = !unitTest;
            return new SinglePlayerWorld(globalData, config, archiveCollection, audioSystem, profiler, geometry,
                mapDef, skillDef, map, sameAsPreviousMap, existingPlayer, worldModel, random, reuse);
        }
        catch (HelionException e)
        {
            Log.Error(e.Message);
        }

        return null;
    }

    private static MapGeometry? GetGeometryAndReset(MapGeometry mapGeometry)
    {
        int count = mapGeometry.Lines.Count;
        for (int i = 0; i < count; i++)
            mapGeometry.Lines[i].Reset();

        count = mapGeometry.Sides.Count;
        for (int i = 0; i < count; i++)
            mapGeometry.Sides[i].Reset();

        count = mapGeometry.Sectors.Count;
        for (int i = 0; i < count; i++)
            mapGeometry.Sectors[i].Reset();

        return mapGeometry;
    }

    private static IList<(FieldInfo, bool)> GetConfigCompatProperties(params (string, bool)[] items)
    {
        List<(FieldInfo, bool)> props = new();
        var type = typeof(ConfigCompat);
        foreach ((string name, bool set) in items)
        {
            var field = type.GetField(name);
            if (field == null)
                continue;
            props.Add((field, set));
        }
        return props;
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
