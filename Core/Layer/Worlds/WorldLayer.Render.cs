using Helion.Geometry.Vectors;
using Helion.Render.Common.Context;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.World;

namespace Helion.Layer.Worlds;

public partial class WorldLayer
{
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
        Vec3F position = oldCamera.Position;
        float yawRadians = oldCamera.YawRadians;
        float pitchRadians = oldCamera.PitchRadians;
        Camera camera = new(position, yawRadians, pitchRadians);

        WorldRenderContext worldContext = new(camera, m_lastTickInfo.Fraction)
        {
            // NOTE: This is temporary because of the old renderer. This will
            // go away when it gets removed.
            DrawAutomap = m_drawAutomap,
            AutomapOffset = m_autoMapOffset,
            AutomapScale = m_autoMapScale
        };

        ctx.World(worldContext, worldRenderer =>
        {
            worldRenderer.Draw(World);
        });

        m_profiler.Render.World.Stop();
    }

    private void DrawAutomapAndHud(IRenderableSurfaceContext ctx)
    {
        m_profiler.Render.Hud.Start();

        HudRenderContext hudContext = new(ctx.Surface.Dimension);
        ctx.Hud(hudContext, hud =>
        {
            if (m_drawAutomap)
            {
                ctx.ClearDepth();
                DrawAutomap(hud);
            }

            ctx.ClearDepth();
            DrawHud(hudContext, hud);
        });

        m_profiler.Render.Hud.Stop();
    }
}
