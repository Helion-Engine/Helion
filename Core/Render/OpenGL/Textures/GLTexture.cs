using Helion;
using Helion.Render;
using Helion.Render.OpenGL;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Textures;

public abstract class GLTexture : IDisposable
{
    protected int Name;
    private bool m_disposed;

    protected GLTexture()
    {
        Name = GL.GenTexture();
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

        GL.DeleteTexture(Name);
        Name = 0;

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
