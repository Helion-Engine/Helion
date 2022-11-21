using Helion.Geometry.Vectors;
using Helion.Render.Common.Context;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.World;

namespace Helion.Layer.Worlds;

public partial class WorldLayer
{
    private readonly WorldCamera m_camera = new(Vec3F.Zero, 0, 0);
    private readonly WorldRenderContext m_worldContext;
    private readonly HudRenderContext m_hudContext;

    public void Render(IRenderableSurfaceContext ctx)
    {
        DrawWorld(ctx);
        DrawAutomapAndHud(ctx);
    }

    private void DrawWorld(IRenderableSurfaceContext ctx)
    {
        m_profiler.Render.World.Start();

        ctx.ClearDepth();
        ctx.ClearStencil();

        // TODO: Workaround until later...
        var oldCamera = World.Player.GetCamera(m_lastTickInfo.Fraction);
        m_camera.Set(oldCamera.Position, oldCamera.YawRadians, oldCamera.PitchRadians);
        m_worldContext.Set(m_lastTickInfo.Fraction, m_drawAutomap, m_autoMapOffset, m_autoMapScale);

        ctx.World(m_worldContext, RenderWorld);
        m_profiler.Render.World.Stop();

        void RenderWorld(IWorldRenderContext context)
        {
            context.Draw(World);
        }
    }

    private void DrawAutomapAndHud(IRenderableSurfaceContext ctx)
    {
        m_profiler.Render.Hud.Start();

        m_hudContext.Set(ctx.Surface.Dimension);
        ctx.Hud(m_hudContext, hud =>
        {
            if (m_drawAutomap)
            {
                ctx.ClearDepth();
                DrawAutomap(hud);
            }

            ctx.ClearDepth();
            DrawHud(m_hudContext, hud, m_drawAutomap);
        });

        m_profiler.Render.Hud.Stop();
    }
}
