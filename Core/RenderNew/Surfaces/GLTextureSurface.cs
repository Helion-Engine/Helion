using System;
using System.Diagnostics;
using Helion.Geometry;
using Helion.RenderNew.OpenGL.Framebuffer;
using Helion.RenderNew.Renderers.Hud;
using Helion.RenderNew.Renderers.World;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.Surfaces;

public class GLTextureSurface : IDisposable
{
    public readonly Dimension Dimension;
    public readonly HudRenderingContext HudRenderer;
    public readonly WorldRenderingContext WorldRenderer;
    public readonly GLFramebuffer Framebuffer;
    private bool m_disposed;

    public GLTextureSurface(string label, Dimension dimension, HudRenderingContext hudRenderer, WorldRenderingContext worldRenderer,
        int numColorAttachments = 0, RenderbufferStorage? storage = null)
    {
        Debug.Assert(dimension.HasPositiveArea, $"Cannot have a {nameof(GLTextureSurface)} with a zero or negative area: {dimension}");

        Dimension = dimension;
        HudRenderer = hudRenderer;
        WorldRenderer = worldRenderer;
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