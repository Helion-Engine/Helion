using System;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;

namespace Helion.RenderNew.Textures;

public class GLTextureManager : IDisposable
{
    private readonly IConfig m_config;
    private readonly ArchiveCollection m_archiveCollection;
    private bool m_disposed;

    public GLTextureManager(IConfig config, ArchiveCollection archiveCollection)
    {
        m_config = config;
        m_archiveCollection = archiveCollection;
    }

    public void Dispose()
    {
        if (m_disposed)
            return;
        
        // TODO

        m_disposed = true;
    }
}