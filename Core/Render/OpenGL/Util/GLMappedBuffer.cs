using System;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Util;

public readonly unsafe struct GLMappedBuffer<T> where T : struct
{
    public readonly IntPtr Pointer;
    public readonly T[] Data;
    public readonly T* MappedMemoryPtr;
    private readonly BufferTarget m_target;
    
    public GLMappedBuffer(T[] data, BufferTarget target, BufferAccessMask access)
    {
        Data = data;
        m_target = target;
        Pointer = GL.MapBufferRange(target, IntPtr.Zero, data.Length, access);
        MappedMemoryPtr = (T*)Pointer.ToPointer();
    }

    public void Dispose()
    {
        GL.UnmapBuffer(m_target);
    }
}