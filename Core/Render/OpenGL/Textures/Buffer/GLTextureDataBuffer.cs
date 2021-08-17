using System;
using Helion.Geometry;
using Helion.Render.OpenGL.Capabilities;
using Helion.Render.OpenGL.Textures.Types;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures.Buffer
{
    public class GLTextureDataBuffer : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly GLTexture2D Texture;
        private bool m_disposed;

        public GLTextureDataBuffer()
        {
            int dim = Math.Min(4096, GLCapabilities.Limits.MaxTexture2DSize);
            Dimension dimension = (dim, dim);
            
            Texture = new GLTexture2D("Texture buffer data", dimension);
        }

        ~GLTextureDataBuffer()
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
            
            Texture.Dispose();

            m_disposed = true;
        }
    }
}
