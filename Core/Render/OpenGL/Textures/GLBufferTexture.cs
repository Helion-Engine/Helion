using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Textures;

public class GLBufferTexture : IDisposable
{
    public readonly string Label;
    private int m_vboName;
    private int m_textureName;
    private bool m_disposed;

    public GLBufferTexture(string label)
    {
        Label = label;
        m_textureName = GL.GenTexture();
        m_vboName = GL.GenBuffer();

        BindBuffer();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.VertexArray, m_vboName, $"TBO (VBO): {label}");
        UnbindBuffer();

        BindTexture();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, m_vboName, $"TBO (Texture): {label}");
        UnbindTexture();
    }

    ~GLBufferTexture()
    {
        Dispose(false);
    }

    public void BindTexture()
    {
        GL.BindTexture(TextureTarget.TextureBuffer, m_textureName);
    }

    public void UnbindTexture()
    {
        GL.BindTexture(TextureTarget.TextureBuffer, 0);
    }

    public void BindBuffer()
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, m_vboName);
    }

    public void UnbindBuffer()
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        GL.DeleteBuffer(m_vboName);
        GL.DeleteTexture(m_textureName);
        m_vboName = 0;
        m_textureName = 0;

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
