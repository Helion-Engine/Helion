using System;
using Helion.Geometry;
using Helion.Render.OpenGL.Framebuffers;
using Helion.Render.OpenGL.Renderers.Hud;
using Helion.Render.OpenGL.Renderers.World;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Surfaces;

/// <summary>
/// A renderable surface backed by a framebuffer texture.
/// </summary>
public class GLRenderableFramebufferTextureSurface : GLRenderableSurface
{
    private readonly GLFramebuffer m_framebuffer;
    private bool m_disposed;

    public override Dimension Dimension => m_framebuffer.Dimension;

    private GLRenderableFramebufferTextureSurface(GLRenderer renderer, GLFramebuffer framebuffer,
        GLHudRenderer hud, GLWorldRenderer world)
        : base(renderer, hud, world)
    {
        m_framebuffer = framebuffer;
    }

    ~GLRenderableFramebufferTextureSurface()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public static GLRenderableFramebufferTextureSurface? Create(GLRenderer renderer, Dimension dimension,
        GLHudRenderer hud, GLWorldRenderer world)
    {
        GLFramebuffer? framebuffer = GLFramebuffer.Create(dimension);
        return framebuffer != null ? new GLRenderableFramebufferTextureSurface(renderer, framebuffer, hud, world) : null;
    }

    protected override void Bind()
    {
        m_framebuffer.Bind();
    }

    protected override void Unbind()
    {
        m_framebuffer.Unbind();
    }

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        PerformDispose();
    }

    private void PerformDispose()
    {
        if (!m_disposed)
            return;

        m_framebuffer.Dispose();

        m_disposed = true;
    }
}

