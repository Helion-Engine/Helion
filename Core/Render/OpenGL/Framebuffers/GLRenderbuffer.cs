using System;
using Helion.Geometry;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Framebuffers;

/// <summary>
/// A renderbuffer that is used with a framebuffer.
/// </summary>
public class GLRenderbuffer : IDisposable
{
    public int RenderbufferName { get; private set; }
    private bool m_disposed;

    public GLRenderbuffer(Dimension dimension)
    {
        RenderbufferName = GL.GenRenderbuffer();

        SetStorage(dimension);
    }

    ~GLRenderbuffer()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public void SetDebugLabel(string name)
    {
        GLUtil.Label($"Renderbuffer: {name}", ObjectLabelIdentifier.Renderbuffer, RenderbufferName);
    }

    private void SetStorage(Dimension dimension)
    {
        Bind();
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, dimension.Width, dimension.Height);
        Unbind();
    }

    public void Bind()
    {
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, RenderbufferName);
    }

    public void Unbind()
    {
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        PerformDispose();
    }

    private void PerformDispose()
    {
        if (m_disposed)
            return;

        GL.DeleteRenderbuffer(RenderbufferName);
        RenderbufferName = 0;

        m_disposed = true;
    }
}
