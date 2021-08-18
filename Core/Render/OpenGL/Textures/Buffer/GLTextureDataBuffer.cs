using System;
using Helion.Geometry;
using Helion.Render.OpenGL.Capabilities;
using Helion.Render.OpenGL.Textures.Types;
using Helion.Resources;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures.Buffer
{
    public class GLTextureDataBuffer : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly GLTexture2D Texture;
        private readonly IResources m_resources;
        private bool m_disposed;

        public GLTextureDataBuffer(IResources resources)
        {
            int dim = Math.Min(4096, GLCapabilities.Limits.MaxTexture2DSize);
            Dimension dimension = (dim, dim);
            
            Texture = new GLTexture2D("Texture buffer data", dimension);
            m_resources = resources;

            UploadResourceData();
        }

        ~GLTextureDataBuffer()
        {
            FailedToDispose(this);
            PerformDispose();
        }
        
        private void UploadResourceData()
        {
            UploadTextures();
        }

        private void UploadTextures()
        {
            // We want a buffer of 2x, since more might get loaded in.
            int expectedSize =  m_resources.Textures.CalculateTotalTextureCount() * 2;
            
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
            
            Texture.Dispose();

            m_disposed = true;
        }
    }
}
