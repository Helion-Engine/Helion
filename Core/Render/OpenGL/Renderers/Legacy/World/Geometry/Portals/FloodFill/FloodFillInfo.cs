using System;
using Helion.Render.OpenGL.Buffer;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

public record FloodFillInfo(int TextureHandle, double Z, RenderableVertices<FloodFillVertex> Vertices) : IDisposable
{
    private bool m_disposed;

    ~FloodFillInfo()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;
        
        Vertices.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}