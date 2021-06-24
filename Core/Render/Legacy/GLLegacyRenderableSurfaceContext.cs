using System;
using System.Drawing;
using Helion.Geometry.Boxes;
using Helion.Render.Common.Context;
using Helion.Render.Common.Renderers;
using Helion.Render.Legacy.Commands;

namespace Helion.Render.Legacy
{
    public class GLLegacyRenderableSurfaceContext : IRenderableSurfaceContext
    {
        public IRenderableSurface Surface { get; }
        public readonly RenderCommands Commands;
        private Box2I m_viewport;

        internal GLLegacyRenderableSurfaceContext(GLLegacyRenderer renderer, GLLegacySurface surface)
        {
            Surface = surface;
            Commands = new RenderCommands(renderer.m_config, renderer.Window.Dimension, 
                renderer.ImageDrawInfoProvider, renderer.m_fpsTracker);
        }

        internal void Begin()
        {
            Commands.Begin();
        }
        
        internal void End()
        {
            // Nothing for now.
        }
        
        public void Clear(Color color, bool depth, bool stencil)
        {
            Commands.Clear(color);

            if (depth)
                Commands.ClearDepth();
            
            if (depth && stencil)
                Commands.Clear();
        }

        public void Viewport(Box2I area)
        {
            m_viewport = area;
            Commands.Viewport(area.Dimension, area.Min);
        }

        public void Viewport(Box2I area, Action action)
        {
            Box2I oldViewport = m_viewport;
            
            Viewport(area);
            action();

            m_viewport = oldViewport;
        }

        public void Scissor(Box2I area)
        {
            // Not part of the old renderer.
        }

        public void Scissor(Box2I area, Action action)
        {
            // Not part of the old renderer.
        }

        public void Hud(HudRenderContext context, Action<IHudRenderContext> action)
        {
            // TODO
        }

        public void World(WorldRenderContext context, Action<IWorldRenderContext> action)
        {
            // TODO
        }
    }
}
