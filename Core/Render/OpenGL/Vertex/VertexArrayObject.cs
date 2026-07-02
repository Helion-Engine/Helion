using System;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Vertex;

public enum VertexArrayType
{
    Legacy,
    Modern
}

public class VertexArrayObject : IDisposable
{
    public readonly int Handle;
    private bool m_disposed;

    public VertexArrayObject(string label, VertexArrayType type = VertexArrayType.Legacy)
    {
        if (type == VertexArrayType.Modern)
            GL.CreateVertexArrays(1, out Handle);
        else
            Handle = GL.GenVertexArray();

        if (GLInfo.DebugLabel)
        {
            Bind();
            GLHelper.ObjectLabel(ObjectLabelIdentifier.VertexArray, Handle, label);
            Unbind();
        }
    }

    ~VertexArrayObject()
    {
        Dispose(false);
    }

    public void Bind()
    {
        GL.BindVertexArray(Handle);
    }

#pragma warning disable CA1822 // Mark members as static
    public void Unbind()
#pragma warning restore CA1822 // Mark members as static
    {
        GL.BindVertexArray(0);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        GL.DeleteVertexArray(Handle);

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
