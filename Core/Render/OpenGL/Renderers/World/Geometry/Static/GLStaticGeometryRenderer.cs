using System;
using Helion.Render.OpenGL.Textures.Buffer;
using Helion.World;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static
{
    public class GLStaticGeometryRenderer : IDisposable
    {
        private readonly GLTextureDataBuffer m_textureDataBuffer;
        private bool m_disposed;

        public GLStaticGeometryRenderer(GLTextureDataBuffer textureDataBuffer)
        {
            m_textureDataBuffer = textureDataBuffer;
        }

        ~GLStaticGeometryRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void UpdateTo(IWorld world)
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
