using System;
using System.Collections.Generic;
using Helion.Audio;
using Helion.Audio.Sounds;
using Helion.Render.Common.Renderers;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Language;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Extensions;
using Helion.Util.Timing;
using Helion.World;
using Helion.World.Entities;
using NLog;

namespace Helion.Layer.EndGame;

public partial class EndGameLayer : IGameLayer
{
    private const int LettersPerSecond = 10;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly IList<string> TheEndImages =
    [
        "END0", "END1", "END2", "END3", "END4", "END5", "END6"
    ];

    private static readonly IList<CastData> Cast =
    [
        new CastData("ZombieMan", "$CC_ZOMBIE"),
        new CastData("ShotgunGuy", "$CC_SHOTGUN"),
        new CastData("ChaingunGuy", "$CC_HEAVY"),
        new CastData("DoomImp", "$CC_IMP"),
        new CastData("Demon", "$CC_DEMON"),
        new CastData("LostSoul", "$CC_LOST"),
        new CastData("Cacodemon", "$CC_CACO"),
        new CastData("HellKnight", "$CC_HELL"),
        new CastData("BaronOfHell", "$CC_BARON"),
        new CastData("Arachnotron", "$CC_ARACH"),
        new CastData("PainElemental", "$CC_PAIN"),
        new CastData("Revenant", "$CC_REVEN"),
        new CastData("Fatso", "$CC_MANCU"),
        new CastData("Archvile", "$CC_ARCH"),
        new CastData("SpiderMastermind", "$CC_SPIDER"),
        new CastData("Cyberdemon", "$CC_CYBER"),
        new CastData("DoomPlayer", "$CC_HERO"),
    ];

    public event EventHandler? Exited;

    public readonly IWorld World;
    public readonly MapInfoDef? NextMapInfo;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly IMusicPlayer m_musicPlayer;
    private readonly SoundManager m_soundManager;
    private readonly string m_backgroundImage;
    private readonly IList<string> m_displayText;
    private readonly Ticker m_ticker = new(LettersPerSecond);
    private readonly Action<HudBackgroundImage> m_virtualDrawBackground;
    private readonly Action<IHudRenderContext> m_virtualDrawCast;
    private readonly Action<HudVirtualText> m_virtualDrawText;
    private readonly Action<HudVirtualBackgroundImage> m_virtualDrawBackgroundImage;
    private EndGameDrawState m_drawState = EndGameDrawState.Text;
    private TimeSpan m_timespan;
    private bool m_shouldScroll;
    private bool m_forceState;
    private int m_xOffset;
    private int m_xOffsetStop;
    private int m_theEndImageIndex;

    private EndGameType m_endGameType;
    private int m_castIndex = -1;
    private bool m_castMelee;
    private bool m_castIsMelee;
    private Entity? m_castEntity;
    private CastEntityState m_castEntityState;
    private int m_castEntityFrameTicks;
    private int m_castFrameCount;

    public EndGameLayer(ArchiveCollection archiveCollection, IMusicPlayer musicPlayer, SoundManager soundManager, IWorld world,
        ClusterDef currentCluster, ClusterDef? nextCluster, MapInfoDef? nextMapInfo, bool isNextMapSecret)
    {
        World = world;
        NextMapInfo = nextMapInfo;
        var language = archiveCollection.Definitions.Language;

        IList<string> clusterText = currentCluster.ExitText.Count > 0 ? currentCluster.ExitText : Array.Empty<string>();
        if (isNextMapSecret)
            clusterText = currentCluster.SecretExitText.Count > 0 ? currentCluster.SecretExitText : Array.Empty<string>();
        if (clusterText.Count == 0 && nextCluster != null && nextCluster.EnterText.Count > 0)
            clusterText = nextCluster.EnterText;

        m_archiveCollection = archiveCollection;
        m_musicPlayer = musicPlayer;
        m_soundManager = soundManager;
        m_backgroundImage = language.GetMessage(currentCluster.Pic);
        if (string.IsNullOrEmpty(m_backgroundImage))
            m_backgroundImage = language.GetMessage(currentCluster.Flat);
        if (string.IsNullOrEmpty(m_backgroundImage))
            m_backgroundImage = archiveCollection.Definitions.MapInfoDefinition.GameDefinition.FinaleFlat;

        m_displayText = LookUpDisplayText(archiveCollection, language, clusterText);
        m_timespan = GetPageTime();

        m_virtualDrawBackground = new(VirtualDrawBackground);
        m_virtualDrawCast = new(VirtualDrawCast);
        m_virtualDrawText = new(VirtualDrawText);
        m_virtualDrawBackgroundImage = new(VirtualDrawBackgroundImage);

        m_ticker.Start();

        if (World.MapInfo.Next.EqualsIgnoreCase("EndGameC") && m_displayText.Count == 0)
        {
            m_endGameType = EndGameType.Cast;
            HandleTextComplete();
            return;
        }

        string music = currentCluster.Music;
        if (string.IsNullOrEmpty(music))
            music = archiveCollection.Definitions.MapInfoDefinition.GameDefinition.FinaleMusic;
        PlayMusic(music);
    }

    private static IList<string> LookUpDisplayText(ArchiveCollection archiveCollection, LanguageDefinition language, IList<string> clusterText)
    {
        if (clusterText.Count == 0)
            return clusterText;

        string lookupText = clusterText[0];
        if (language.TryGetMessages(lookupText, out var messages))
            return messages;

        var entry = archiveCollection.FindEntry(lookupText);
        if (entry != null)
            return LanguageDefinition.SplitMessageByNewLines(entry.ReadDataAsString());

        return clusterText;
    }

    private TimeSpan GetPageTime() =>
        TimeSpan.FromSeconds(m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.PageTime);

    private void PlayMusic(string music)
    {
        m_musicPlayer.Stop();
        if (music.Empty())
            return;

        music = m_archiveCollection.Definitions.Language.GetMessage(music);

        Entry? entry = m_archiveCollection.Entries.FindByName(music);
        if (entry == null)
        {
            Log.Warn($"Cannot find end game music file: {music}");
            return;
        }

        m_musicPlayer.Play(entry.ReadData());
    }

    public void Dispose()
    {
        // TODO: Anything we need to dispose of? What about `Exited`?
    }
}
