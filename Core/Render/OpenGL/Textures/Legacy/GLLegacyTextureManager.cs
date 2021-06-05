using System;
using System.Collections.Generic;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures.Legacy
{
    public class GLLegacyTextureManager : IGLTextureManager
    {
        public GLTextureHandle NullHandle { get; }
        public GLFontTextureHandle NullFontHandle { get; }
        private readonly ArchiveCollection m_archiveCollection;
        private readonly LegacyCubeGLTexture m_cubeTexture;
        private readonly List<GLTextureHandle> m_handles = new();
        private readonly List<GLFontTextureHandle> m_fontHandles = new();
        private bool m_disposed;

        public GLLegacyTextureManager(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
            m_cubeTexture = new LegacyCubeGLTexture();
            
            // TODO: Make null texture.
            NullHandle = m_handles[0];
            
            // TODO: Make font handle.
            NullFontHandle = m_fontHandles[0];
        }

        ~GLLegacyTextureManager()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public GLTextureHandle Get(string name, ResourceNamespace priority)
        {
            // TODO
            return NullHandle;
        }

        public GLTextureHandle Get(Texture texture)
        {
            // TODO
            return NullHandle;
        }

        public GLFontTextureHandle GetFont(string name)
        {
            // TODO
            return NullFontHandle;
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

            m_handles.Clear();
            m_fontHandles.Clear();
            m_cubeTexture.Dispose();

            m_disposed = true;
        }
    }
}
