using Helion.Input;
using Helion.Render.Commands;
using Helion.Render.Shared.Drawers;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Timing;

namespace Helion.Layer
{
    public class EndGameLayer : GameLayer
    {
        private readonly ClusterDef m_cluster;
        private readonly MapInfoDef m_nextMap;
        private readonly Ticker m_ticker = new(Constants.TicksPerSecond);
        private readonly EndGameDrawer m_drawer;
        private bool m_playingMusic;

        protected override double Priority => 0.675;

        public EndGameLayer(ArchiveCollection archiveCollection, ClusterDef cluster, MapInfoDef nextMap)
        {
            m_drawer = new(archiveCollection);
            m_cluster = cluster;
            m_nextMap = nextMap;
            
            m_ticker.Start();
        }
        
        private void FinishEndGame()
        {
            // TODO: If we're not on the image pic and there is one, go to that.
            // TODO: Go to the next map.
        }

        public override void HandleInput(InputEvent input)
        {
            if (input.HasAnyKeyPressed())
                FinishEndGame();
            
            base.HandleInput(input);
        }

        public override void RunLogic()
        {
            if (!m_playingMusic)
            {
                // TODO: Play music
                m_playingMusic = true;
            }
            
            base.RunLogic();
        }

        public override void Render(RenderCommands renderCommands)
        {
            m_drawer.Draw(m_cluster, m_ticker, renderCommands);
            
            base.Render(renderCommands);
        }
    }
}
