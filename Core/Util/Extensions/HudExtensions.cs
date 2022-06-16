using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;

namespace Helion.Util.Extensions;

public static class HudExtensions
{
    public static bool RenderFullscreenImage(this IHudRenderContext hud, string image, 
        Align window = Align.TopLeft, Align anchor = Align.TopLeft)
    {
        if (!hud.Textures.TryGet(image, out var handle))
            return false;

        if (handle.Dimension.AspectRatio == 1.6f)
        {
            hud.VirtualDimension(handle.Dimension, ResolutionScale.Center, Constants.DoomVirtualAspectRatio, () =>
            {
                hud.Image(image, (0, 0, handle.Dimension.Width, handle.Dimension.Height), window, anchor);
            });
            return true;
        }

        hud.VirtualDimension(handle.Dimension, ResolutionScale.Center, handle.Dimension.AspectRatio, () =>
        {
            hud.Image(image, (0, 0, handle.Dimension.Width, handle.Dimension.Height), window, anchor);
        });
        return true;
    }

    public static bool RenderStatusBar(this IHudRenderContext hud, string image)
    {
        if (!hud.Textures.TryGet(image, out var handle))
            return false;

        float statusBarRatio = handle.Dimension.Width * 2 / 480f;
        hud.VirtualDimension((handle.Dimension.Width, 200), ResolutionScale.Center, statusBarRatio, () =>
        {
            hud.Image(image, (0, 0, handle.Dimension.Width, handle.Dimension.Height), both: Align.BottomLeft);
        });
        return true;
    }
}
