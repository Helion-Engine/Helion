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

    private Action<IHudRenderContext> m_drawAutomapAndHudAction;
    private readonly Action<IWorldRenderContext> m_renderWorldAction;

    const int FullSizeHudOffsetY = 16;

    public void Render(IRenderableSurfaceContext ctx)
    {
        var offset = GetViewPortOffset(ctx.Surface.Dimension);
        bool viewportOffset = offset.X != 0 || offset.Y != 0;

        if (viewportOffset)
        {
            var box = new Box2I((offset.X, offset.Y), (ctx.Surface.Dimension.Width + offset.X, ctx.Surface.Dimension.Height + offset.Y));
            ctx.Viewport(box);
        }
 
        DrawWorld(ctx);

        if (viewportOffset)
            ctx.Viewport(ctx.Surface.Dimension.Box);

        DrawAutomapAndHud(ctx);
    }

    private void DrawWorld(IRenderableSurfaceContext ctx)
    {
        m_profiler.Render.World.Start();

        ctx.ClearDepth();
        ctx.ClearStencil();

        // TODO: Workaround until later...
        var oldCamera = World.GetCameraPlayer().GetCamera(m_lastTickInfo.Fraction);
        m_camera.Set(oldCamera.PositionInterpolated, oldCamera.Position, oldCamera.YawRadians, oldCamera.PitchRadians);
        m_worldContext.Set(m_lastTickInfo.Fraction, m_drawAutomap, m_autoMapOffset, m_autoMapScale);

        ctx.World(m_worldContext, m_renderWorldAction);
        m_profiler.Render.World.Stop();
    }

    void RenderWorld(IWorldRenderContext context)
    {
        context.Draw(World);
    }

    private void DrawAutomapAndHud(IRenderableSurfaceContext ctx)
    {
        m_profiler.Render.Hud.Start();

        m_hudContext.Set(ctx.Surface.Dimension);
        m_renderableHudContext = ctx;
        ctx.Hud(m_hudContext, m_drawAutomapAndHudAction);

        m_profiler.Render.Hud.Stop();
    }

    private void DrawAutomapAndHudContext(IHudRenderContext hud)
    {
        if (m_renderableHudContext == null)
            return;

        if (m_drawAutomap)
        {
            m_renderableHudContext.ClearDepth();
            DrawAutomap(hud);
        }

        m_renderableHudContext.ClearDepth();
        DrawHud(m_hudContext, hud, m_drawAutomap);
    }

    private Vec2I GetViewPortOffset(Dimension viewport)
    {
        if (m_config.Hud.StatusBarSize == StatusBarSizeType.Full)
            return (0, (int)(viewport.Height / 200.0 * FullSizeHudOffsetY));
        return (0, 0);
    }
}
