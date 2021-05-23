using System;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Modern.Textures
{
    /// <summary>
    /// A custom managed texture that is not tracked in the texture manager.
    /// </summary>
    /// <remarks>
    /// Some times we want to create a texture but not have it tracked, and
    /// instead track it on our own. This handle is a way for this to be done
    /// while still registering it with code in the texture manager.
    /// </remarks>
    public class ModernGLTextureHandle : IDisposable
    {
        public readonly ModernGLTexture Texture;
        private bool m_disposed;

        internal ModernGLTextureHandle(ModernGLTexture texture)
        {
            Texture = texture;
        }
        
        ~ModernGLTextureHandle()
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
