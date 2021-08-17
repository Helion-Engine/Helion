using System;
using Helion.Render.Common.Context;
using Helion.World;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.World.Geometry
{
    public class GLGeometryRenderer : IGLWorldRenderer
    {
        private bool m_disposed;
        
        ~GLGeometryRenderer()
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
