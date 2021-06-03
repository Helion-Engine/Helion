using System;
using Helion.Geometry;
using Helion.Render.Common.Framebuffer;

namespace Helion.Render.Legacy
{
    public class GLLegacyFramebuffer : IFramebuffer
    {
        public string Name => IFramebuffer.DefaultName;
        public Dimension Dimension => m_window.Dimension;
        private readonly IWindow m_window;

        public GLLegacyFramebuffer(IWindow window)
        {
            m_window = window;
        }

        public void Render(Action<FramebufferRenderContext> action)
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
