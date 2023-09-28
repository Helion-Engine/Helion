using System;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Queries;

public class GLQuery : IDisposable
{
    protected int m_objectId { get; private set; }
    private bool m_disposed;

    public GLQuery(string label)
    {
        m_objectId = GL.GenQuery();

        GLHelper.ObjectLabel(ObjectLabelIdentifier.Query, m_objectId, $"Query: {label}");
    }

    ~GLQuery()
    {
        ReleaseUnmanagedResources();
    }

    private void ReleaseUnmanagedResources()
    {
        if (m_disposed)
            return;

        GL.DeleteQuery(m_objectId);

        m_disposed = true;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}
