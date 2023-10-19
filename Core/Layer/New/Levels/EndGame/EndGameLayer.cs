using System;
using System.Collections.Generic;
using Helion.Audio;
using Helion.Audio.Sounds;
using Helion.Layer.New.Util;
using Helion.Render.Common.Renderers;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Language;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Extensions;
using Helion.Util.Sounds.Mus;
using Helion.Util.Timing;
using Helion.World;
using Helion.World.Entities;
using NLog;

namespace Helion.Layer.New.Levels.EndGame;

public partial class EndGameLayer : GameLayer
{
    private const int LettersPerSecond = 10;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public static readonly IEnumerable<string> EndGameMaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "EndPic", "EndGame1", "EndGame2", "EndGameW", "EndGame4", "EndGameC", "EndGame3",
        "EndDemon", "EndGameS", "EndChess", "EndTitle", "EndSequence", "EndBunny", "EndGame"
    };
    private static readonly IList<string> TheEndImages = new[]
    {
        "END0", "END1", "END2", "END3", "END4", "END5", "END6"
    };
    
    public readonly IWorld World;
    public readonly MapInfoDef? NextMapInfo;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly IMusicPlayer m_musicPlayer;
    private readonly SoundManager m_soundManager;
    private readonly string m_flatImage;
    private readonly IList<string> m_displayText;
    private readonly Ticker m_ticker = new(LettersPerSecond);
    private readonly Action<IHudRenderContext> m_virtualDrawBackground;
    private readonly Action<IHudRenderContext> m_virtualDrawCast;
    private readonly Action<HudVirtualText> m_virtualDrawText;
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
    
    public event EventHandler? Exited;

    protected override double Priority => (double)LayerPriority.EndGame;
    
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
        m_flatImage = language.GetMessage(currentCluster.Flat);
        m_displayText = LookUpDisplayText(archiveCollection, language, clusterText);
        m_timespan = GetPageTime();

        m_virtualDrawBackground = VirtualDrawBackground;
        m_virtualDrawCast = VirtualDrawCast;
        m_virtualDrawText = VirtualDrawText;

        m_ticker.Start();
        string music = currentCluster.Music;
        if (music == "")
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

    private TimeSpan GetPageTime()
    {
        return TimeSpan.FromSeconds(m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.PageTime);
    }

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

        byte[] data = entry.ReadData();
        // Eventually we'll need to not assume .mus all the time.
        byte[]? midiData = MusToMidi.Convert(data);

        if (midiData != null)
            m_musicPlayer.Play(midiData);
        else
            Log.Warn($"Cannot decode end game music file: {music}");
    }
    
    public override bool? ShouldFocus()
    {
        return true;
    }
}