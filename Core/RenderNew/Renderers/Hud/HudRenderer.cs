using System;
using Helion.RenderNew.Textures;
using Helion.Util.Configs;

namespace Helion.RenderNew.Renderers.Hud;

public class HudRenderer : IDisposable
{
    public readonly HudRenderingContext Context;
    private bool m_disposed;

    public HudRenderer(IConfig config, GLTextureManager textureManager)
    {
        Context = new(config, textureManager, this);
    }
    
    public void Dispose()
    {
        if (m_disposed)
            return;
        
        Context.Dispose();

        m_disposed = true;
        GC.SuppressFinalize(this);
    }
}