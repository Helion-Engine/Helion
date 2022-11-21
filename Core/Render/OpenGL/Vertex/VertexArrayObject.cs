using System;
using Helion;
using Helion.Render;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Util;
using Helion.Render.OpenGL.Vertex;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Vertex;

public class VertexArrayObject : IDisposable
{
    public readonly VertexArrayAttributes Attributes;
    private readonly IGLFunctions gl;
    private readonly int m_vaoId;

    public VertexArrayObject(GLCapabilities capabilities, IGLFunctions functions, VertexArrayAttributes vaoAttributes,
        string objectLabel = "")
    {
        gl = functions;
        Attributes = vaoAttributes;
        m_vaoId = gl.GenVertexArray();

        Bind();
        GLHelper.ObjectLabel(gl, capabilities, ObjectLabelType.VertexArray, m_vaoId, objectLabel);
        Unbind();
    }

    ~VertexArrayObject()
    {
        FailedToDispose(this);
        ReleaseUnmanagedResources();
    }

    public void Bind()
    {
        gl.BindVertexArray(m_vaoId);
    }

    public void Unbind()
    {
        gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources()
    {
        gl.DeleteVertexArray(m_vaoId);
    }
}
