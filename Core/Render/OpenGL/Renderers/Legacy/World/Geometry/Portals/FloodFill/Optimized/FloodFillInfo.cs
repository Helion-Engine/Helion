using System;
using Helion.Render.OpenGL.Buffer;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill.Optimized;

public record FloodFillInfo(int TextureHandle, double Z, RenderableVertices<FloodFillVertex> Vertices) : IDisposable
{
    ~FloodFillInfo()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        Vertices.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}