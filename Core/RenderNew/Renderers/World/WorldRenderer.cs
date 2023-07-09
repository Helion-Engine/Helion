using System;

namespace Helion.RenderNew.Renderers.World;

public class WorldRenderer : IDisposable
{
    private bool m_disposed;
    
    public void Dispose()
    {
        if (m_disposed)
            return;

        m_disposed = true;
        GC.SuppressFinalize(this);
    }
}