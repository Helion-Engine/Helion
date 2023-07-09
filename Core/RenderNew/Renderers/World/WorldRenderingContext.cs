using System;

namespace Helion.RenderNew.Renderers.World;

public class WorldRenderingContext : IDisposable
{
    private bool m_currentlyRendering;
    private bool m_disposed;

    public virtual void Begin()
    {
        if (m_currentlyRendering)
            throw new($"Did not call {nameof(Begin)}() for {nameof(WorldRenderingContext)}");

        m_currentlyRendering = true;
    }
    
    public virtual void End()
    {
        if (!m_currentlyRendering)
            throw new($"Did not call {nameof(End)}() for {nameof(WorldRenderingContext)}");
        
        m_currentlyRendering = false;
    }

    public void Dispose()
    {
        if (m_disposed)
            return;
        
        // TODO

        if (m_currentlyRendering)
            throw new($"Trying to dispose {nameof(WorldRenderingContext)} while actively rendering");
        
        m_disposed = true;
    }
}