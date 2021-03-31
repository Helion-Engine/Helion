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
using NLog;

namespace Helion.Layer
{
    public class EndGameLayer : GameLayer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly string m_flatImage;
        private readonly List<string> m_displayText;
        private readonly ClusterDef m_cluster;
        private readonly Ticker m_ticker = new(Constants.TicksPerSecond);
        private readonly EndGameDrawer m_drawer;
        private readonly Action m_nextMapFunc;
        private bool m_invokedNextMapFunc;

        protected override double Priority => 0.675;

        public EndGameLayer(ArchiveCollection archiveCollection, IMusicPlayer musicPlayer, ClusterDef cluster,
            Action nextMapFunc)
        {
            var language = archiveCollection.Definitions.Language;
            
            m_drawer = new(archiveCollection);
            m_cluster = cluster;
            m_nextMapFunc = nextMapFunc;
            m_flatImage = language.GetDefaultMessage(cluster.Flat);
            m_displayText = LookUpDisplayText(language, cluster);
            
            m_ticker.Start();
            PlayMusic(archiveCollection, musicPlayer, cluster.Music);
        }

        private static List<string> LookUpDisplayText(LanguageDefinition language, ClusterDef cluster)
        {
            return cluster.ExitText.Count != 1 ?
                cluster.ExitText :
                language.GetDefaultMessage(cluster.ExitText[0]).Split(",").ToList();
        }

        private void PlayMusic(ArchiveCollection archiveCollection, IMusicPlayer musicPlayer, string clusterMusic)
        {
            if (clusterMusic.Empty())
            {
                musicPlayer.Stop();
                return;
            }
            
            Entry? entry = archiveCollection.Entries.FindByName(clusterMusic);
            if (entry == null)
            {
                Log.Warn($"Cannot find end game music file: {clusterMusic}");
                return;
            }

            byte[] data = entry.ReadData();
            // Eventually we'll need to not assume .mus all the time.
            byte[]? midiData = MusToMidi.Convert(data);

            if (midiData != null)
                musicPlayer.Play(midiData);
            else
                Log.Warn($"Cannot decode end game music file: {clusterMusic}");
        }

        private void FinishEndGame()
        {
            if (m_invokedNextMapFunc) 
                return;
            
            m_invokedNextMapFunc = true;
            m_nextMapFunc();
        }

        public override void HandleInput(InputEvent input)
        {
            if (input.HasAnyKeyPressed())
                FinishEndGame();
            
            base.HandleInput(input);
        }

        public override void Render(RenderCommands renderCommands)
        {
            m_drawer.Draw(m_cluster, m_flatImage, m_ticker, renderCommands);
            
            base.Render(renderCommands);
        }
    }
}
