using System;
using Helion.Render.Common.Context;
using Helion.World;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.World.Bsp
{
    /// <summary>
    /// A renderer that walks the BSP tree for deciding what to render and in
    /// what order.
    /// </summary>
    /// <remarks>
    /// This is not related to the GLBSP tool.
    /// </remarks>
    public class GLBspWorldRenderer : IGLWorldRenderer
    {
        private bool m_disposed;
        
        ~GLBspWorldRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void Draw(IWorld world)
        {
            // TODO
        }
        
        public void Render(WorldRenderContext context)
        {
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
