using System;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Vertex.New;

public class VertexArrayObject : IDisposable
{
    private readonly int m_name;
    private bool m_disposed;

    public VertexArrayObject(string label)
    {
        m_name = GL.GenVertexArray();

        Bind();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.VertexArray, m_name, label);
        Unbind();
    }

    ~VertexArrayObject()
    {
        Dispose(false);
    }

    public void Bind()
    {
        GL.BindVertexArray(m_name);
    }

    public void Unbind()
    {
        GL.BindVertexArray(0);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        GL.DeleteVertexArray(m_name);

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
