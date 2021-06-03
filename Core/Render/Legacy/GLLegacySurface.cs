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

        public GLLegacySurface(IWindow window)
        {
            m_window = window;
        }
        
        public void Render(Action<IRenderableSurfaceContext> action)
        {
            // TODO
        }

        public void Dispose()
        {
            // Nothing to dispose of.
            GC.SuppressFinalize(this);
        }
    }
}
