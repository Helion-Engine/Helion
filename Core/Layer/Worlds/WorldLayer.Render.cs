using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Context;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.World;
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

    public void RenderWorld(IRenderableSurfaceContext ctx)
    {
        if (m_drawAutomap)
            return;

        m_profiler.Render.World.Start();

        ctx.ClearDepth();
        ctx.ClearStencil();

        var oldCamera = World.GetCameraPlayer().GetCamera(m_lastTickInfo.Fraction);
        m_camera.Set(oldCamera.PositionInterpolated, oldCamera.Position, oldCamera.YawRadians, oldCamera.PitchRadians);
        m_worldContext.Set(m_lastTickInfo.Fraction, m_drawAutomap, m_autoMapOffset, m_autoMapScale);

        ctx.World(m_worldContext, m_renderWorldAction);
        m_profiler.Render.World.Stop();
    }

    public void RenderAutomap(IRenderableSurfaceContext ctx)
    {
        if (!m_drawAutomap)
            return;

        ctx.ClearDepth();
        ctx.ClearStencil();

        var oldCamera = World.GetCameraPlayer().GetCamera(m_lastTickInfo.Fraction);
        m_camera.Set(oldCamera.PositionInterpolated, oldCamera.Position, oldCamera.YawRadians, oldCamera.PitchRadians);
        m_worldContext.Set(m_lastTickInfo.Fraction, m_drawAutomap, m_autoMapOffset, m_autoMapScale);

        ctx.World(m_worldContext, m_renderWorldAction);
    }

    void RenderWorld(IWorldRenderContext context)
    {
        context.Draw(World);
    }

    public void RenderHud(IRenderableSurfaceContext ctx)
    {
        m_profiler.Render.Hud.Start();

        m_hudContext.Set(ctx.Surface.Dimension);
        m_renderableHudContext = ctx;
        ctx.Hud(m_hudContext, m_drawHudAction);

        m_profiler.Render.Hud.Stop();
    }

    private void DrawHudContext(IHudRenderContext hud)
    {
        if (m_renderableHudContext == null)
            return;

        m_renderableHudContext.ClearDepth();
        DrawHud(m_hudContext, hud, m_drawAutomap);
    }
}
