using System;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Queries;

public class GLQuery : IDisposable
{
    protected readonly int Name;
    private readonly string m_label;
    private bool m_disposed;

    public GLQuery(string label)
    {
        Name = GL.GenQuery();
        m_label = label;

        GLHelper.ObjectLabel(ObjectLabelIdentifier.Query, Name, $"Query: {m_label}");
    }

    ~GLQuery()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        GL.DeleteQuery(Name);

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
