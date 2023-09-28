using System;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Buffers;

/// <summary>
/// A wrapper around a data source that forces us to access with mapped data in
/// a way that is less error prone. Since C# doesn't have RAII, this is the next
/// best way to avoid disastrous forgetting-to-unmap errors.
/// </summary>
/// <remarks>
/// This is intended to be a singleton object so there is no GC pressure from
/// mapping.
/// </remarks>
public unsafe class MappedBuffer<T> : IDisposable where T : struct
{
    public bool Mapped { get; private set; }
    private readonly BufferObject<T> m_bufferObject;
    private readonly BufferTarget m_target;
    private IntPtr m_intPtr;
    private T* m_mappedMemoryPtr;
    private bool m_disposed;

    public MappedBuffer(BufferObject<T> bufferObject, BufferTarget target)
    {
        m_bufferObject = bufferObject;
        m_target = target;
    }

    ~MappedBuffer()
    {
        ReleaseUnmanagedResources();
    }

    private T this[int index]
    {
        get => m_bufferObject.m_data[index];
        set
        {
            m_bufferObject.m_data[index] = value;
            m_mappedMemoryPtr[index] = value;
        }
    }

    public void Map(BufferAccessMask mask = BufferAccessMask.MapWriteBit)
    {
        if (Mapped)
            throw new("Trying to map a GL buffer twice, likely forgot to unmap");

        m_intPtr = GL.MapBufferRange(m_target, 0, m_bufferObject.m_data.Length, mask);
        m_mappedMemoryPtr = (T*)m_intPtr.ToPointer();
        Mapped = true;
    }

    public void MapUnsynchronized()
    {
        Map(BufferAccessMask.MapWriteBit | BufferAccessMask.MapUnsynchronizedBit);
    }

    public void Unmap()
    {
        if (!Mapped)
            throw new("Trying to unmap a GL buffer when it was not mapped");

        GL.UnmapBuffer(m_target);
        Mapped = false;
    }

    private void ReleaseUnmanagedResources()
    {
        if (m_disposed)
            return;

        if (Mapped)
            Unmap();

        m_disposed = true;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}