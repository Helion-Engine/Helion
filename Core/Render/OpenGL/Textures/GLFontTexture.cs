using Helion.Graphics.Fonts;
using System;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures
{
    /// <summary>
    /// A texture for a font.
    /// </summary>
    public class GLFontTexture : IDisposable
    {
        public readonly GLTexture Texture;
        private readonly IFont m_font;
        private bool m_disposed;
        
        public GLFontTexture(GLTexture texture, IFont font)
        {
            Texture = texture;
            m_font = font;
        }

        ~GLFontTexture()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        protected virtual void PerformDispose()
        {
            if (m_disposed)
                return;

            Texture.Dispose();

            m_disposed = true;
        }
    }
}
