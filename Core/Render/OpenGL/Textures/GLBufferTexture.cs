using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Helion.Render.OpenGL.Textures;

// See: https://gist.github.com/roxlu/5090067
public class GLBufferTexture<T> : IDisposable where T : struct
{
    public readonly string Label;
    public readonly bool PersistentBufferStorage;
    private readonly T[] m_data;
    private readonly int m_name;
    private readonly int m_textureName;
    private readonly SizedInternalFormat m_format;
    private bool m_disposed;

    public GLBufferTexture(string label, T[] data, SizedInternalFormat format, bool persistentBufferStorage)
    {
        Label = label;
        m_name = GL.GenBuffer();
        m_textureName = GL.GenTexture();
        
        m_data = data;
        m_format = format;
        PersistentBufferStorage = persistentBufferStorage;

        BindBuffer();

        var size = Unsafe.SizeOf<T>();
        if (persistentBufferStorage)
            GL.BufferStorage(BufferTarget.TextureBuffer, data.Length * size, 0, BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit);
        else
            GL.BufferData(BufferTarget.TextureBuffer, data.Length * size, m_data, BufferUsageHint.DynamicDraw);

        if (GLInfo.DebugLabel)
            GLHelper.ObjectLabel(ObjectLabelIdentifier.Buffer, m_name, $"TBO: {label}");
        UnbindBuffer();
    }

    ~GLBufferTexture()
    {
        Dispose(false);
    }

    private MapBufferAccessMask GetAccess()
    {
        if (PersistentBufferStorage)
            return MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapUnsynchronizedBit | MapBufferAccessMask.MapPersistentBit;
        return MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapUnsynchronizedBit;
    }

    public void Map(Action<IntPtr> action)
    {
        Debug.Assert(!m_disposed, "Trying to use a mapped pointer when it's been disposed");
        
        BindBuffer();
        
        GLMappedBuffer<T> buffer = new(m_data, BufferTarget.TextureBuffer, GetAccess());
        action(buffer.Pointer);
        buffer.Dispose();

        UnbindBuffer();
    }
    
    // You must bind, and call dispose, or else bad things will happen.
    public GLMappedBuffer<T> MapWithDisposable()
    {
        Debug.Assert(!m_disposed, "Trying to use a mapped pointer when it's been disposed");
        return new(m_data, BufferTarget.TextureBuffer, GetAccess());
    }

    public void BindBuffer()
    {
        GL.BindBuffer(BufferTarget.TextureBuffer, m_name);
    }
    
    public static void UnbindBuffer()
    {
        GL.BindBuffer(BufferTarget.TextureBuffer, 0);
    }
    
    public void BindTexture()
    {
        GL.BindTexture(TextureTarget.TextureBuffer, m_textureName);
    }
    
    public static void UnbindTexture()
    {
        GL.BindTexture(TextureTarget.TextureBuffer, 0);
    }

    public void BindTexBuffer()
    {
        GL.TexBuffer(TextureBufferTarget.TextureBuffer, m_format, m_name);
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
