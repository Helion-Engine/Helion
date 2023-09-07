using System;
using System.Diagnostics;
using Helion.Geometry;
using Helion.RenderNew.OpenGL.Framebuffer;
using Helion.RenderNew.Renderers.Hud;
using Helion.RenderNew.Renderers.World;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.Surfaces;

public class GLTextureSurface : IGLSurface, IDisposable
{
    public readonly GLFramebuffer Framebuffer;
    private bool m_disposed;

    public Dimension Dimension { get; }
    public HudRenderingContext Hud { get; }
    public WorldRenderingContext World { get; }

    public GLTextureSurface(string label, Dimension dimension, HudRenderingContext hud, WorldRenderingContext world, 
        int numColorAttachments = 0, RenderbufferStorage? storage = null)
    {
        Debug.Assert(dimension.HasPositiveArea, $"Cannot have a {nameof(GLTextureSurface)} with a zero or negative area: {dimension}");

        Dimension = dimension;
        Framebuffer = new(label, dimension, numColorAttachments, storage);
        Hud = hud;
        World = world;
    }

    public void Bind()
    {
        Framebuffer.Bind();
    }

    public void Unbind()
    {
        Framebuffer.Unbind();
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