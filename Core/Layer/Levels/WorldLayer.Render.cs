using System;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Context;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.World;

namespace Helion.Layer.Levels;

public partial class WorldLayer
{
    private readonly Camera m_camera = new(Vec3F.Zero, 0, 0);
    private readonly WorldRenderContext m_worldContext;
    private readonly HudRenderContext m_hudContext;
    private readonly Action<IHudRenderContext> m_drawAutomapAndHudAction;
    private readonly Action<IWorldRenderContext> m_renderWorldAction;
    private IRenderableSurfaceContext? m_renderableHudContext;

    public override void Render(IRenderableSurfaceContext surface, IHudRenderContext ctx)
    {
        DrawWorld(surface);
        DrawAutomapAndHud(surface);
    }

    private void DrawWorld(IRenderableSurfaceContext surface)
    {
        m_profiler.Render.World.Start();

        surface.ClearDepth();
        surface.ClearStencil();

        // TODO: Workaround until later...
        var oldCamera = World.GetCameraPlayer().GetCamera(m_lastTickInfo.Fraction);
        m_camera.Set(oldCamera.PositionInterpolated, oldCamera.Position, oldCamera.YawRadians, oldCamera.PitchRadians);
        m_worldContext.Set(m_lastTickInfo.Fraction, m_drawAutomap, m_autoMapOffset, m_autoMapScale);

        surface.World(m_worldContext, m_renderWorldAction);
        m_profiler.Render.World.Stop();
    }

    void RenderWorld(IWorldRenderContext context)
    {
        context.Draw(World);
    }

    private void DrawAutomapAndHud(IRenderableSurfaceContext surface)
    {
        m_profiler.Render.Hud.Start();

        m_hudContext.Set(surface.Surface.Dimension);
        m_renderableHudContext = surface;
        surface.Hud(m_hudContext, m_drawAutomapAndHudAction);

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
}
