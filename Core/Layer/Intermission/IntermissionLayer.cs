using Helion.Audio;
using Helion.Audio.Sounds;
using Helion.Layer.Intermission;
using Helion.Render.Common.Renderers;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Intermission;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Parser;
using Helion.World;
using Helion.World.Stats;
using NLog;
using System;
using System.Text.RegularExpressions;

namespace Helion.Layer.Worlds;

public partial class IntermissionLayer : IGameLayer
{
    private const int StatAddAmount = 2;
    private const int TimeAddAmount = 3;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public readonly IWorld World;
    public readonly MapInfoDef CurrentMapInfo;
    public readonly MapInfoDef? NextMapInfo;
    public readonly Func<FindMapResult> GetNextMapInfo;
    public int LevelTimeSeconds { get; private set; }
    public int ParTimeSeconds { get; private set; }
    public string IntermissionPic { get; private set; }
    public IntermissionDef? IntermissionDef { get; private set; }
    public IntermissionState IntermissionState { get; private set; } = IntermissionState.Started;
    private readonly GameLayerManager m_gameLayerManager;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly SoundManager m_soundManager;
    private readonly IMusicPlayer m_musicPlayer;
    private readonly LevelStats m_levelPercents = new();
    private readonly int m_totalLevelTime;
    private readonly Action<IHudRenderContext> m_renderVirtualIntermissionAction;
    private IntermissionState m_delayState;
    private int m_tics;
    private int m_delayStateTics;
    private readonly IConfigKeyMapping m_keys;
    private int m_textUpscalingFactor;

    public event EventHandler? Exited;

    public double KillPercent => m_levelPercents.KillCount;
    public double ItemPercent => m_levelPercents.ItemCount;
    public double SecretPercent => m_levelPercents.SecretCount;
    private bool IsNextMap => IntermissionState == IntermissionState.NextMap;

    public IntermissionLayer(GameLayerManager parent, IWorld world, IConfigKeyMapping keys, SoundManager soundManager,
        IMusicPlayer musicPlayer, MapInfoDef currentMapInfo, Func<FindMapResult> getNextMapInfo, int textUpscalingFactor)
    {
        m_gameLayerManager = parent;
        World = world;
        CurrentMapInfo = currentMapInfo;
        GetNextMapInfo = getNextMapInfo;
        NextMapInfo = getNextMapInfo().MapInfo;
        m_archiveCollection = world.ArchiveCollection;
        m_soundManager = soundManager;
        m_musicPlayer = musicPlayer;
        m_totalLevelTime = World.LevelTime / (int)Constants.TicksPerSecond;
        m_renderVirtualIntermissionAction = new(RenderVirtualIntermission);
        m_keys = keys;
        m_textUpscalingFactor = textUpscalingFactor;

        IntermissionPic = string.IsNullOrEmpty(currentMapInfo.ExitPic) ? "INTERPIC" : currentMapInfo.ExitPic;
        CalculatePercentages();
        CheckEpisodeIntermission();

        m_delayStateTics = (int)Constants.TicksPerSecond;
        m_delayState = IntermissionState.TallyingKills;

        PlayIntermissionMusic();
    }

    private void CalculatePercentages()
    {
        if (World.LevelStats.TotalMonsters != 0)
            m_levelPercents.TotalMonsters = (World.LevelStats.KillCount * 100) / World.LevelStats.TotalMonsters;
        if (World.LevelStats.TotalItems != 0)
            m_levelPercents.TotalItems = (World.LevelStats.ItemCount * 100) / World.LevelStats.TotalItems;
        if (World.LevelStats.TotalSecrets != 0)
            m_levelPercents.TotalSecrets = (World.LevelStats.SecretCount * 100) / World.LevelStats.TotalSecrets;
    }

    private void CheckEpisodeIntermission()
    {
        if (!Regex.IsMatch(CurrentMapInfo.MapName, @"E\dM\d"))
            return;

        string fileName = $"in_epi{CurrentMapInfo.MapName[1]}";
        var entry = m_archiveCollection.Entries.FindByName(fileName);
        if (entry == null)
            return;

        try
        {
            IntermissionDef = IntermissionParser.Parse(entry.ReadDataAsString());
            IntermissionPic = IntermissionDef.Background;

            foreach (var def in IntermissionDef.Animations)
                def.ShouldDraw = def.Type == IntermissionAnimationType.Normal;
        }
        catch (ParserException e)
        {
            Log.Error(e);
        }
    }

    private void PlayIntermissionMusic()
    {
        m_musicPlayer.Stop();

        string musicName = m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.IntermissionMusic;
        musicName = m_archiveCollection.Definitions.Language.GetMessage(musicName);

        Entry? entry = m_archiveCollection.Entries.FindByName(musicName);
        if (entry == null)
        {
            Log.Error($"Cannot find intermission music: {musicName}");
            return;
        }

        m_musicPlayer.Play(entry.ReadData());
    }

    public void Dispose()
    {
        // Not used.
    }
}
