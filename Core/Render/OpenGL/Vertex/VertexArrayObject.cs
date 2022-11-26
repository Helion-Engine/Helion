using System;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Vertex;

public class VertexArrayObject : IDisposable
{
    public readonly VertexArrayAttributes Attributes;
    private readonly int m_vaoId;

    public VertexArrayObject(VertexArrayAttributes vaoAttributes, string objectLabel)
    {
        m_vaoId = GL.GenVertexArray();
        Attributes = vaoAttributes;

        Bind();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.VertexArray, m_vaoId, objectLabel);
        Unbind();
    }

    ~VertexArrayObject()
    {
        ReleaseUnmanagedResources();
    }

    public void Bind()
    {
        GL.BindVertexArray(m_vaoId);
    }

    public void Unbind()
    {
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources()
    {
        GL.DeleteVertexArray(m_vaoId);
    }
}
