using System;

namespace Helion.Render.OpenGL.Texture.New;

public abstract class GLTexture : IDisposable
{
    protected int Name;
    private bool m_disposed;

    protected GLTexture()
    {

    }

    ~GLTexture()
    {
        Dispose(false);
    }

    public abstract void Bind();
    public abstract void Unbind();

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        // TODO

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
