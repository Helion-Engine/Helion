using System;
using Helion.Geometry;
using Helion.Render.Common.Renderers;
using Helion.Render.OpenGL.Renderers.Hud;
using Helion.Render.OpenGL.Renderers.World;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Surfaces
{
    public abstract class GLRenderableSurface : IRenderableSurface
    {
        protected readonly GLRenderer Renderer;
        protected readonly GLHudRenderer HudRenderer;
        protected readonly GLWorldRenderer WorldRenderer;
        private bool m_disposed;
        
        public abstract Dimension Dimension { get; }

        protected GLRenderableSurface(GLRenderer renderer, GLHudRenderer hud, GLWorldRenderer world)
        {
            Renderer = renderer;
            HudRenderer = hud;
            WorldRenderer = world;
        }

        ~GLRenderableSurface()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        protected abstract void Bind();
        
        protected abstract void Unbind();
        
        public virtual void Render(Action<IRenderableSurfaceContext> action) 
        {
            (int w, int h) = Dimension;
            GL.Viewport(0, 0, w, h);
            GL.Scissor(0, 0, w, h);
            
            Bind();
            
            // TODO: Re-use one instead of constantly reconstructing one.
            GLRenderableSurfaceContext context = new(this, HudRenderer, WorldRenderer);
            action(context);
            
            Unbind();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        protected virtual void PerformDispose()
        {
            if (m_disposed)
                return;

            HudRenderer.Dispose();
            WorldRenderer.Dispose();
            
            m_disposed = true;
        }
    }
}
