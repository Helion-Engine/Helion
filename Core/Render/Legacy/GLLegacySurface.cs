using System;
using Helion.Geometry;
using Helion.Render.Common.Renderers;

namespace Helion.Render.Legacy
{
    public class GLLegacySurface : IRenderableSurface
    {
        public string Name => IRenderableSurface.DefaultName;
        public Dimension Dimension => m_window.Dimension;
        private readonly IWindow m_window;
        private readonly GLLegacyRenderableSurfaceContext ctx;

        public GLLegacySurface(IWindow window, GLLegacyRenderer renderer)
        {
            m_window = window;
            ctx = new GLLegacyRenderableSurfaceContext(renderer, this);
        }
        
        public void Render(Action<IRenderableSurfaceContext> action)
        {
            ctx.Begin();
            action(ctx);
            ctx.End();
        }

        public void Dispose()
        {
            // Nothing to dispose of.
            GC.SuppressFinalize(this);
        }
    }
}
