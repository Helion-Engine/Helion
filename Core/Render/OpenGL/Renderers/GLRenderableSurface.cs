using System;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Render.Common.Renderers;
using Helion.Render.OpenGL.Renderers.Hud;
using Helion.Render.OpenGL.Renderers.World;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers
{
    public class GLRenderableSurface : IRenderableSurface
    {
        private readonly GLRenderer m_renderer;
        private readonly GLHudRenderer m_hudRenderer;
        private readonly GLWorldRenderer m_worldRenderer;
        private Box2I m_viewport;
        private Box2I m_scissor;
        private bool m_disposed;

        public Dimension Dimension => m_renderer.Window.Dimension;

        public GLRenderableSurface(GLRenderer renderer, Dimension dimension, GLHudRenderer hud, GLWorldRenderer world)
        {
            m_renderer = renderer;
            m_viewport = ((0, 0), Dimension.Vector);
            m_scissor = ((0, 0), Dimension.Vector);
            m_hudRenderer = hud;
            m_worldRenderer = world;
        }

        ~GLRenderableSurface()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void Render(Action<IRenderableSurfaceContext> action)
        {
            GLRenderableSurfaceContext context = new(this, m_hudRenderer, m_worldRenderer);
            
            // TODO
            action(context);
            // TODO
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;

            // TODO
            
            m_disposed = true;
        }
    }
}
