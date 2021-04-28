using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Audio;
using Helion.Input;
using Helion.Render.Commands;
using Helion.Render.Shared.Drawers;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Language;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Sounds.Mus;
using Helion.Util.Timing;
using Helion.World;
using NLog;

namespace Helion.Layer
{
    public class EndGameLayer : GameLayer
    {
        private const int LettersPerSecond = 10;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static readonly IEnumerable<string> EndGameMaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EndPic", "EndGame1", "EndGame2", "EndGameW", "EndGame4", "EndGameC", "EndGame3",
            "EndDemon", "EndGameS", "EndChess", "EndTitle", "EndSequence", "EndBunny"
        };

        public event EventHandler? Exited;

        public IWorld World { get; private set; }
        public MapInfoDef? NextMapInfo { get; private set; }

        private readonly string m_flatImage;
        private readonly List<string> m_displayText;
        private readonly Ticker m_ticker = new(LettersPerSecond);
        private readonly EndGameDrawer m_drawer;
        private bool m_showAllText;
        private bool m_invokedNextMapFunc;

        protected override double Priority => 0.675;

        public EndGameLayer(ArchiveCollection archiveCollection, IMusicPlayer musicPlayer, IWorld world,
            ClusterDef cluster, MapInfoDef? nextMapInfo)
        {
            World = world;
            NextMapInfo = nextMapInfo;
            var language = archiveCollection.Definitions.Language;
            
            m_drawer = new(archiveCollection);
            m_flatImage = language.GetMessage(cluster.Flat);
            m_displayText = LookUpDisplayText(language, cluster);
            
            m_ticker.Start();
            PlayMusic(archiveCollection, musicPlayer, cluster, language);
        }

        private static List<string> LookUpDisplayText(LanguageDefinition language, ClusterDef cluster)
        {
            if (cluster.ExitText.Count != 1)
                return cluster.ExitText;
            
            string message = language.GetMessage(cluster.ExitText[0]);
            return message.Split("\n").ToList();
        }

        private static void PlayMusic(ArchiveCollection archiveCollection, IMusicPlayer musicPlayer, ClusterDef cluster,
            LanguageDefinition language)
        {
            string music = cluster.Music;
            if (music.Empty())
                music = archiveCollection.Definitions.MapInfoDefinition.GameDefinition.FinaleMusic;

            musicPlayer.Stop();
            if (music.Empty())
                return;

            music = language.GetMessage(music);
            
            Entry? entry = archiveCollection.Entries.FindByName(music);
            if (entry == null)
            {
                Log.Warn($"Cannot find end game music file: {music}");
                return;
            }

            byte[] data = entry.ReadData();
            // Eventually we'll need to not assume .mus all the time.
            byte[]? midiData = MusToMidi.Convert(data);

            if (midiData != null)
                musicPlayer.Play(midiData);
            else
                Log.Warn($"Cannot decode end game music file: {music}");
        }

        private void AdvanceState()
        {
            if (!m_showAllText)
            {
                m_showAllText = true;
                return;
            }
            
            if (m_invokedNextMapFunc) 
                return;
            
            m_invokedNextMapFunc = true;
            Exited?.Invoke(this, EventArgs.Empty);
        }

        public override void HandleInput(InputEvent input)
        {
            if (input.HasAnyKeyPressed())
                AdvanceState();
            
            base.HandleInput(input);
        }

        public override void Render(RenderCommands renderCommands)
        {
            m_drawer.Draw(m_flatImage, m_displayText, m_ticker, m_showAllText, renderCommands);
            
            base.Render(renderCommands);
        }
    }
}
