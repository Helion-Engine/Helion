using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using System.Reflection.Metadata;

namespace Helion.Util.Extensions;

public static class HudExtensions
{
    private readonly record struct HudImage(IHudRenderContext Hud, string Image, IRenderableTextureHandle Handle, Align Window, Align Anchor);

    public static bool RenderFullscreenImage(this IHudRenderContext hud, string image, 
        Align window = Align.TopLeft, Align anchor = Align.TopLeft)
    {
        if (!hud.Textures.TryGet(image, out var handle))
            return false;

        if (handle.Dimension.AspectRatio == 1.6f)
        {
            hud.VirtualDimension(handle.Dimension, ResolutionScale.Center, Constants.DoomVirtualAspectRatio, HudVirtualFullscreenImage,
                new HudImage(hud, image, handle, window, anchor));
            return true;
        }

        hud.VirtualDimension(handle.Dimension, ResolutionScale.Center, handle.Dimension.AspectRatio, HudVirtualFullscreenImage,
            new HudImage(hud, image, handle, window, anchor));
        return true;
    }

    public static bool RenderStatusBar(this IHudRenderContext hud, string image)
    {
        if (!hud.Textures.TryGet(image, out var handle))
            return false;

        float statusBarRatio = handle.Dimension.Width * 2 / 480f;
        hud.VirtualDimension((handle.Dimension.Width, 200), ResolutionScale.Center, statusBarRatio, HudVirtualStatusBar,
            new HudImage(hud, image, handle, Align.Center, Align.Center));
        return true;
    }

    private static void HudVirtualFullscreenImage(HudImage hud)
    {
        hud.Hud.Image(hud.Image, (0, 0, hud.Handle.Dimension.Width, hud.Handle.Dimension.Height), hud.Window, hud.Anchor);
    }

    private static void HudVirtualStatusBar(HudImage hud)
    {
        hud.Hud.Image(hud.Image, (0, 0, hud.Handle.Dimension.Width, hud.Handle.Dimension.Height), both: Align.BottomLeft);
    }
}
