using System;
using System.Diagnostics;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Textures;

// See: https://gist.github.com/roxlu/5090067
public class GLBufferTexture : IDisposable
{
    public readonly string Label;
    private readonly float[] m_data;
    private readonly int m_name;
    private readonly int m_textureName;
    private bool m_disposed;
    private bool m_persistentBufferStorage;

    public GLBufferTexture(string label, float[] data, bool persistentBufferStorage)
    {
        Label = label;
        m_name = GL.GenBuffer();
        m_textureName = GL.GenTexture();
        
        m_data = data;
        m_persistentBufferStorage = persistentBufferStorage;

        BindBuffer();

        if (persistentBufferStorage)
            GL.BufferStorage(BufferTarget.TextureBuffer, data.Length * 4, 0, BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit);
        else
            GL.BufferData(BufferTarget.TextureBuffer, data.Length * 4, m_data, BufferUsageHint.DynamicDraw);
        
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Buffer, m_name, $"TBO: {label}");
        UnbindBuffer();
    }

    ~GLBufferTexture()
    {
        Dispose(false);
    }

    private BufferAccessMask GetAccess()
    {
        if (m_persistentBufferStorage)
            return BufferAccessMask.MapWriteBit | BufferAccessMask.MapUnsynchronizedBit | BufferAccessMask.MapPersistentBit;
        return BufferAccessMask.MapWriteBit | BufferAccessMask.MapUnsynchronizedBit;
    }

    public void Map(Action<IntPtr> action)
    {
        Debug.Assert(!m_disposed, "Trying to use a mapped pointer when it's been disposed");
        
        BindBuffer();
        
        GLMappedBuffer<float> buffer = new(m_data, BufferTarget.TextureBuffer, GetAccess());
        action(buffer.Pointer);
        buffer.Dispose();
        
        UnbindBuffer();
    }
    
    // You must bind, and call dispose, or else bad things will happen.
    public GLMappedBuffer<float> MapWithDisposable()
    {
        Debug.Assert(!m_disposed, "Trying to use a mapped pointer when it's been disposed");
        return new(m_data, BufferTarget.TextureBuffer, GetAccess());
    }

    public void BindBuffer()
    {
        GL.BindBuffer(BufferTarget.TextureBuffer, m_name);
    }
    
    public void UnbindBuffer()
    {
        GL.BindBuffer(BufferTarget.TextureBuffer, 0);
    }
    
    public void BindTexture()
    {
        GL.BindTexture(TextureTarget.TextureBuffer, m_textureName);
    }
    
    public void UnbindTexture()
    {
        GL.BindTexture(TextureTarget.TextureBuffer, 0);
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
