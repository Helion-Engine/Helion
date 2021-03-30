using System;
using Helion.Audio;
using Helion.Input;
using Helion.Render.Commands;
using Helion.Render.Shared.Drawers;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
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

        private readonly ClusterDef m_cluster;
        private readonly MapInfoDef m_nextMap;
        private readonly Ticker m_ticker = new(Constants.TicksPerSecond);
        private readonly EndGameDrawer m_drawer;
        private readonly Action m_nextMapFunc;
        private bool m_drawingPic;
        private bool m_invokedNextMapFunc;

        protected override double Priority => 0.675;

        public EndGameLayer(ArchiveCollection archiveCollection, IMusicPlayer musicPlayer, ClusterDef cluster, 
            MapInfoDef nextMap, Action nextMapFunc)
        {
            m_drawer = new(archiveCollection);
            m_cluster = cluster;
            m_nextMap = nextMap;
            m_nextMapFunc = nextMapFunc;
            
            m_ticker.Start();
            PlayMusic(archiveCollection, musicPlayer, cluster.Music);
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
                Log.Warn($"Cannot find music file: {clusterMusic}");
                return;
            }

            byte[] data = entry.ReadData();
            // Eventually we'll need to not assume .mus all the time.
            byte[]? midiData = MusToMidi.Convert(data);

            if (midiData != null)
                musicPlayer.Play(midiData);
            else
                Log.Warn($"Cannot decode music file: {clusterMusic}");
        }

        private void FinishEndGame()
        {
            if (!m_drawingPic && !m_cluster.Pic.Empty())
                m_drawingPic = true;
            else if (!m_invokedNextMapFunc)
            {
                m_invokedNextMapFunc = true;
                m_nextMapFunc();
            }
        }

        public override void HandleInput(InputEvent input)
        {
            if (input.HasAnyKeyPressed())
                FinishEndGame();
            
            base.HandleInput(input);
        }

        public override void Render(RenderCommands renderCommands)
        {
            m_drawer.Draw(m_cluster, m_drawingPic, m_ticker, renderCommands);
            
            base.Render(renderCommands);
        }
    }
}
