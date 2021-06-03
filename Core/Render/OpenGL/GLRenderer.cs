using System;
using Helion.Geometry;
using Helion.Render.Common.Framebuffer;
using Helion.Resources.Archives.Collection;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL
{
    public class GLRenderer : IRenderer
    {
        public IWindow Window { get; }
        public IFramebuffer Default { get; }
        private readonly ArchiveCollection m_archiveCollection;
        private bool m_disposed;

        public GLRenderer(IWindow window, ArchiveCollection archiveCollection)
        {
            Window = window;
            m_archiveCollection = archiveCollection;
        }

        ~GLRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public IFramebuffer GetOrCreateFrameBuffer(string name, Dimension dimension)
        {
            // TODO
            return null;
        }

        public IFramebuffer? GetFrameBuffer(string name)
        {
            // TODO
            return null;
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
