using System;
using Helion.RenderNew.Textures;
using Helion.Util.Configs;

namespace Helion.RenderNew.Renderers.World;

public class WorldRenderer : IDisposable
{
    public readonly WorldRenderingContext Context;
    private bool m_disposed;

    public WorldRenderer(IConfig config, GLAtlasTextureManager textureManager)
    {
        Context = new(config, textureManager, this);
    }

    public void Dispose()
    {
        if (m_disposed)
            return;

        m_disposed = true;
        GC.SuppressFinalize(this);
    }
}