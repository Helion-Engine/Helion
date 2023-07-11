using System;
using System.Diagnostics;
using Helion.Geometry;
using Helion.RenderNew.OpenGL.Framebuffer;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.Surfaces;

public class GLTextureSurface : IDisposable
{
    public readonly Dimension Dimension;
    public readonly GLFramebuffer Framebuffer;
    private bool m_disposed;

    public GLTextureSurface(string label, Dimension dimension, int numColorAttachments = 0, RenderbufferStorage? storage = null)
    {
        Debug.Assert(dimension.HasPositiveArea, $"Cannot have a {nameof(GLTextureSurface)} with a zero or negative area: {dimension}");

        Dimension = dimension;
        Framebuffer = new(label, dimension, numColorAttachments, storage);
    }

    ~GLTextureSurface()
    {
        Dispose(false);
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    protected void Dispose(bool disposing)
    {
        if (m_disposed)
            return;
        
        Framebuffer.Dispose();

        m_disposed = true;
    }
}