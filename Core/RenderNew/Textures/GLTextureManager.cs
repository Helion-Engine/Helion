using System;
using Helion.RenderNew.OpenGL.Textures;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;

namespace Helion.RenderNew.Textures;

public class GLTextureManager : IDisposable
{
    public readonly ImmutableGLTexture2D NullTexture;
    public readonly ImmutableGLTexture2D WhiteTexture;
    private readonly IConfig m_config;
    private readonly ArchiveCollection m_archiveCollection;
    private bool m_disposed;

    public GLTextureManager(IConfig config, ArchiveCollection archiveCollection)
    {
        m_config = config;
        m_archiveCollection = archiveCollection;
        
        // TODO: Set NullTexture
        // TODO: Set WhiteTexture
    }

    public bool Get(string name, out ImmutableGLTexture2D texture)
    {
        texture = NullTexture;
        // TODO
        return false;
    }

    public void Dispose()
    {
        if (m_disposed)
            return;
        
        // TODO

        m_disposed = true;
    }
}