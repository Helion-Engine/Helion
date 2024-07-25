using Helion.Geometry.Vectors;
using Helion.Render.Common.Context;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.World;
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

    private RenderHudOptions m_renderHudOptions;

    public void RenderWorld(IRenderableSurfaceContext ctx)
    {
        m_profiler.Render.World.Start();

        ctx.ClearDepth();
        ctx.ClearStencil();

        SetWorldContextVars();

        ctx.World(m_worldContext, m_renderWorldAction);
        m_profiler.Render.World.Stop();
    }

    public void RenderAutomap(IWorldRenderContext ctx)
    {
        m_profiler.Render.Automap.Start();

        SetWorldContextVars();

        ctx.DrawAutomap(World);
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
