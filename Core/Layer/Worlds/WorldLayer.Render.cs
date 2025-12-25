using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common.Context;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.World;
using Helion.Resources.Definitions.StatusBar;
using Helion.World.StatusBar;
using System;

namespace Helion.Layer.Worlds;

public partial class WorldLayer
{
    private readonly Camera m_camera = new(Vec3F.Zero, 0, 0);
    private readonly WorldRenderContext m_worldContext;
    private readonly HudRenderContext m_hudContext;
    private IRenderableSurfaceContext? m_renderableHudContext;

    private Action<IHudRenderContext> m_drawHudAction;
    private readonly Action<IWorldRenderContext> m_renderWorldAction;
    private readonly Action<IWorldRenderContext> m_renderAutomapAction;
    private readonly Action m_renderWorldViewportAction;
    private IRenderableSurfaceContext? m_renderableWorldSurfaceContext;

    private RenderHudOptions m_renderHudOptions;

    public void RenderWorld(IRenderableSurfaceContext ctx)
    {
        m_profiler.Render.World.Start();

        ctx.ClearDepth();
        ctx.ClearStencil();

        SetWorldContextVars();

        StatusBarLayoutDef? activeSbar = GetActiveStatusBarLayout();
        int sbarHeight = activeSbar != null
            ? (activeSbar.FullscreenRender ? 0 : activeSbar.Height)
            : (m_config.Hud.StatusBarSize.Value == StatusBarSizeType.Full ? 32 : 0);

        int nativeWidth = ctx.Surface.Dimension.Width;
        int nativeHeight = ctx.Surface.Dimension.Height;

        int nativeBarHeight = (int)(nativeHeight / 200.0 * sbarHeight);

        int viewportBottom = nativeBarHeight;
        int viewportHeight = nativeHeight - viewportBottom;
        Box2I viewportArea = new((0, viewportBottom), (nativeWidth, nativeHeight));

        m_worldContext.Viewport = (nativeWidth, viewportHeight);

        m_renderableWorldSurfaceContext = ctx;
        ctx.Viewport(viewportArea, m_renderWorldViewportAction);
        m_renderableWorldSurfaceContext = null;

        m_worldContext.Viewport = ctx.Surface.Dimension;

        m_profiler.Render.World.Stop();
    }

    public void RenderAutomap(IWorldRenderContext worldCtx)
    {
        m_profiler.Render.Automap.Start();

        SetWorldContextVars();

        worldCtx.DrawAutomap(World);
        m_profiler.Render.Automap.Stop();
    }

    private void SetWorldContextVars()
    {
        var oldCamera = World.GetCameraPlayer().GetCamera(m_lastTickInfo.Fraction);
        m_camera.Set(oldCamera.PositionInterpolated, oldCamera.Position, oldCamera.YawRadians, oldCamera.PitchRadians);
        m_worldContext.Set(m_lastTickInfo.Fraction, DrawAutomap, m_autoMapOffset, m_autoMapScale);
    }

    void RenderWorld(IWorldRenderContext context)
    {
        context.Draw(World);
    }
    
    private void RenderWorldViewportContext()
    {
        m_renderableWorldSurfaceContext?.World(m_worldContext, m_renderWorldAction);
    }

    public void RenderHud(IRenderableSurfaceContext ctx, RenderHudOptions options)
    {
        m_renderHudOptions = options;
        m_profiler.Render.Hud.Start();

        m_hudContext.Set(ctx.Surface.Dimension);
        m_renderableHudContext = ctx;
        ctx.Hud(m_hudContext, m_drawHudAction);

        m_profiler.Render.Hud.Stop();
    }

    public void RenderAutomap(IRenderableSurfaceContext ctx)
    {
        if (!m_config.Hud.AutoMap.Overlay)
        {
            Color color = new(m_config.Hud.AutoMap.BackgroundColor.Value);
            ctx.Clear(color, true, true);
        }

        ctx.Automap(m_worldContext, m_renderAutomapAction);
    }

    private void DrawHudContext(IHudRenderContext hud)
    {
        if (m_renderableHudContext == null)
            return;

        m_renderableHudContext.ClearDepth();
        DrawHud(m_hudContext, hud, DrawAutomap);
    }
}
