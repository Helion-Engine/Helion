using System;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.World.Bsp
{
    /// <summary>
    /// A renderer that walks the BSP tree.
    /// </summary>
    /// <remarks>
    /// This is not related to the GLBSP tool.
    /// </remarks>
    public class GLBspWorldRenderer : GLWorldRenderer
    {
        private bool m_disposed;
        
        internal override GLWorldRenderContext Context { get; }

        public GLBspWorldRenderer()
        {
            Context = new GLBspWorldRenderContext();
        }

        ~GLBspWorldRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }
        
        public override void Dispose()
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
