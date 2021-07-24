using Helion.Geometry.Vectors;
using Helion.Render.Common.Context;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.World;

namespace Helion.Layer.Worlds
{
    public partial class WorldLayer
    {
        public void Render(IRenderableSurfaceContext ctx)
        {
            DrawWorld(ctx);
            DrawAutomapAndHud(ctx);
        }

        private void DrawWorld(IRenderableSurfaceContext ctx)
        {
            ctx.ClearDepth();
            ctx.ClearStencil();
            
            // TODO: Workaround until later...
            var oldCamera = World.Player.GetCamera(m_lastTickInfo.Fraction);
            Vec3F position = oldCamera.Position;
            float yawRadians = oldCamera.YawRadians;
            float pitchRadians = oldCamera.PitchRadians;
            Camera camera = new(position, yawRadians, pitchRadians);
            
            WorldRenderContext worldContext = new(camera, m_lastTickInfo.Fraction);
            ctx.World(worldContext, worldRenderer =>
            {
                worldRenderer.Draw(World);
            });
        }

        private void DrawAutomapAndHud(IRenderableSurfaceContext ctx)
        {
            HudRenderContext hudContext = new(ctx.Surface.Dimension);
            ctx.Hud(hudContext, hud =>
            {
                ctx.ClearDepth();
                DrawAutomap(hud);

                ctx.ClearDepth();
                DrawHud(hudContext, hud);
            });
        }
    }
}
