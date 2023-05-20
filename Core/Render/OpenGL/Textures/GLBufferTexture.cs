using OpenTK.Graphics.OpenGL;
using Helion.Render.OpenGL.Util;
using Helion.Util.Container;
using System;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Textures;

// See: https://gist.github.com/roxlu/5090067
public class GLBufferTexture : IDisposable
{
    public readonly string Label;
    private readonly DynamicArray<float> m_data = new();
    private readonly int m_name;
    private readonly int m_textureName;
    private bool m_disposed;

    public GLBufferTexture(string label, int size)
    {
        Debug.Assert(size > 0, "Cannot have a buffer texture with no size");
        
        Label = label;
        m_name = GL.GenBuffer();
        m_textureName = GL.GenTexture();
        
        m_data.Resize(size);

        BindBuffer();
        GL.BufferData(BufferTarget.TextureBuffer, size, m_data.Data, BufferUsageHint.StaticDraw);
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Buffer, m_name, $"TBO: {label}");
        UnbindBuffer();
    }

    ~GLBufferTexture()
    {
        Dispose(false);
    }

    public void Map(Action<IntPtr> action)
    {
        Debug.Assert(!m_disposed, "Trying to use a mapped pointer when it's been disposed");
        
        BindBuffer();
        
        GLMappedBuffer<float> buffer = new(m_data.Data);
        action(buffer.Pointer);
        buffer.Dispose();
        
        UnbindBuffer();
    }

    public void BindBuffer()
    {
        GL.BindBuffer(BufferTarget.TextureBuffer, m_name);
    }
    
    public void UnbindBuffer()
    {
        GL.BindBuffer(BufferTarget.TextureBuffer, 0);
    }

    public void BindTexBuffer()
    {
        GL.TexBuffer(TextureBufferTarget.TextureBuffer, SizedInternalFormat.R32f, m_name);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        GL.DeleteBuffer(m_name);
        GL.DeleteTexture(m_textureName);

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
