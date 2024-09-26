using Helion.Graphics;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Resources;
using System;

namespace Helion.Util.Extensions;

public static class HudExtensions
{
    private readonly record struct HudImage(IHudRenderContext Hud, string Image, IRenderableTextureHandle Handle, Align Window, Align Anchor, float Alpha = 1f);

    public static IRenderableTextureHandle CreateImage(this IHudRenderContext hud, Image image, string imageName, ResourceNamespace resourceNamespace, out Action removeAction, bool repeatY = true)
    {
        return hud.Textures.CreateAndTrackTexture(imageName, resourceNamespace, image, out removeAction, repeatY: repeatY);
    }

    public static bool RenderFullscreenImage(this IHudRenderContext hud, string image,
        Align window = Align.TopLeft, Align anchor = Align.TopLeft, float alpha = 1f, float aspectRatioDivisor = 1.2f)
    {
        if (!hud.Textures.TryGet(image, out var handle))
            return false;

        hud.VirtualDimension(handle.Dimension, ResolutionScale.Center, handle.Dimension.AspectRatio / aspectRatioDivisor, HudVirtualFullscreenImage,
            new HudImage(hud, image, handle, window, anchor, alpha));
        return true;
    }

    public static bool RenderStatusBar(this IHudRenderContext hud, string image)
    {
        if (!hud.Textures.TryGet(image, out var handle))
            return false;

        float statusBarRatio = handle.Dimension.Width * 2 / 480f;
        if (hud.Dimension.Width < 480)
            statusBarRatio = handle.Dimension.Width * hud.Dimension.AspectRatio / (float)hud.Dimension.Width;

        hud.VirtualDimension((handle.Dimension.Width, 200), ResolutionScale.Center, statusBarRatio, HudVirtualStatusBar,
            new HudImage(hud, image, handle, Align.Center, Align.Center));
        return true;
    }

    private static void HudVirtualFullscreenImage(HudImage hud)
    {
        hud.Hud.Image(hud.Image, (0, 0, hud.Handle.Dimension.Width, hud.Handle.Dimension.Height), hud.Window, hud.Anchor, alpha: hud.Alpha);
    }

    private static void HudVirtualStatusBar(HudImage hud)
    {
        hud.Hud.Image(hud.Image, (0, 0, hud.Handle.Dimension.Width, hud.Handle.Dimension.Height), both: Align.BottomLeft);
    }
}
