using System;
using System.Diagnostics;
using Helion.Geometry;
using Helion.RenderNew.OpenGL.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.OpenGL.Framebuffer;

public class GLRenderbuffer : IDisposable
{
    public readonly string Label;
    public int Name;
    private bool m_disposed;

    public GLRenderbuffer(string label, Dimension dimension, RenderbufferStorage storage = RenderbufferStorage.Depth24Stencil8)
    {
        Debug.Assert(dimension.Area > 0, $"Must have a positive dimension for renderbuffer {label}");

        Label = label;
        Name = GL.GenRenderbuffer();

        Bind();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Renderbuffer, Name, $"Renderbuffer: {Label}");
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, storage, dimension.Width, dimension.Height);
        Unbind();
    }

    ~GLRenderbuffer()
    {
        Dispose(false);
    }

    public void Bind()
    {
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, Name);
    }

    public void Unbind()
    {
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        GL.DeleteRenderbuffer(Name);
        Name = 0;

        m_disposed = true;
    }
}
