using Helion.Graphics;
using Helion.Render.Common.Renderers;
using Helion.Util.Timing;
using Helion.Window;

namespace Helion.Layer.Images;

public class ImageLayer : IGameLayer
{
    public string Image { get; protected set; }

    public ImageLayer(string image)
    {
        Image = image;
    }

    public virtual void HandleInput(IConsumableInput input)
    {
        // Not used.
    }

    public virtual void RunLogic(TickerInfo tickerInfo)
    {
        // Not used.
    }

    public virtual void Render(IHudRenderContext hud)
    {
        hud.Clear(Color.Black);

        hud.DoomVirtualResolution((int value) =>
        {
            hud.Image(Image, (0, 0));
        }, 0);
    }

    public void Dispose()
    {
        // Nothing to dispose.
    }
}
