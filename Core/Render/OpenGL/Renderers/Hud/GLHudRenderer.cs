using System;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Hud
{
    public class GLHudRenderer : IDisposable
    {
        internal readonly GLHudRenderContext Context;
        private bool m_disposed;

        public GLHudRenderer()
        {
            Context = new GLHudRenderContext();
        }

        ~GLHudRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
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

            Context.Dispose();

            m_disposed = true;
        }
    }
}
