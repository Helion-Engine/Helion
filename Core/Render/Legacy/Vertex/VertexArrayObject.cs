using System;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;
using Helion.Render.Legacy.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Legacy.Vertex;

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

        BindAnd(() => { GLHelper.ObjectLabel(gl, capabilities, ObjectLabelType.VertexArray, m_vaoId, objectLabel); });
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

    public void BindAnd(Action action)
    {
        Bind();
        action.Invoke();
        Unbind();
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

