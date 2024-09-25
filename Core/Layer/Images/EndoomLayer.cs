namespace Helion.Layer.Images
{
    using Helion.Graphics;
    using Helion.Render.Common.Renderers;
    using Helion.Util.Extensions;
    using Helion.Util.Timing;
    using Helion.Window;
    using System;

    public class EndoomLayer : IGameLayer
    {
        Action m_closeAction;

        public EndoomLayer(Action closeAction)
        {
            m_closeAction = closeAction;
        }

        public void Dispose()
        {
        }

        public void HandleInput(IConsumableInput input)
        {
            if (input.HasAnyKeyPressed())
            {
                m_closeAction();
            }
        }

        public void RunLogic(TickerInfo tickerInfo)
        {

        }


        public virtual void Render(IHudRenderContext hud)
        {
            hud.Clear(Color.Black);

            // Just a test
            hud.RenderFullscreenImage("FIREBLU1");
        }
    }
}
