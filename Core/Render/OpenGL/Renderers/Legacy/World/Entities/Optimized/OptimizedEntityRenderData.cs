using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities.Optimized;

public class OptimizedEntityRenderData : IDisposable
{
    private bool m_disposed;

    public OptimizedEntityRenderData()
    {
        // TODO
    }

    ~OptimizedEntityRenderData()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        // TODO

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

public class OptimizedEntityRenderDataManager
{

}
