using System;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Util;

// Should only be used if you know what you're doing. Forgetting to dispose is catastrophic.
public readonly unsafe ref struct GLMappedBuffer<T> where T : struct
{
    public readonly IntPtr Pointer;
    private readonly Span<T> m_span;
    private readonly T* m_mappedMemoryPtr;
    
    public GLMappedBuffer(Span<T> span)
    {
        m_span = span;
        Pointer = GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.WriteOnly);
        m_mappedMemoryPtr = (T*)Pointer.ToPointer();
    }
    
    public T this[int index]
    {
        get => m_span[index];
        set
        {
            m_span[index] = value; // We want to write locally so we can debug it.
            m_mappedMemoryPtr[index] = value; // But our writes are primarily intended to go to the mapped buffer.
        }
    }

    public void Dispose()
    {
        GL.UnmapBuffer(BufferTarget.ArrayBuffer);
    }
}