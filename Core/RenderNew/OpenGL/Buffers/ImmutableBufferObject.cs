using System;
using System.Runtime.InteropServices;
using Helion.RenderNew.OpenGL.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.OpenGL.Buffers;

public abstract class ImmutableBufferObject<T> : IDisposable where T : struct
{
    private const BufferStorageFlags WritePersistentCoherent = BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit | BufferStorageFlags.MapCoherentBit; 
    private static readonly int BytesPerElement = Marshal.SizeOf<T>();

    public readonly string Label;
    protected readonly T[] Data;
    private readonly int m_name;
    private readonly BufferTarget m_target;
    private bool m_disposed;

    protected ImmutableBufferObject(string label, BufferTarget target, int size, BufferStorageFlags flags = WritePersistentCoherent)
    {
        Label = label;
        m_name = GL.GenBuffer();
        Data = new T[size];
        m_target = target;
        
        Bind();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Buffer, m_name, Label);
        GL.BufferStorage(target, BytesPerElement * size, Data, flags);
        Unbind();
    }

    ~ImmutableBufferObject()
    {
        Dispose(false);
    }

    public T this[int index]
    {
        get => Data[index];
        set => Data[index] = value;
    }
    
    public void Bind()
    {
        GL.BindBuffer(m_target, m_name);
    }

    public void Unbind()
    {
        GL.BindBuffer(m_target, 0);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;
        
        ReleaseUnmanagedResources();
        
        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources()
    {
        GL.DeleteBuffer(m_name);
    }
}