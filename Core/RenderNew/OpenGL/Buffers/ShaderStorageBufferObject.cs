using System;
using System.Runtime.InteropServices;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.OpenGL.Buffers;

public class ShaderStorageBufferObject<T> : BufferObject<T> where T : struct
{
    private static readonly int BytesPerElement = Marshal.SizeOf<T>();
    private readonly BufferUsageHint m_hint;
    
    public ShaderStorageBufferObject(string label, int size, BufferUsageHint hint) : 
        base(label, BufferTarget.ShaderStorageBuffer, hint, size)
    {
        m_hint = hint;
    }

    public unsafe void Update(ReadOnlySpan<T> data, int elementOffset)
    {
        int offsetBytes = elementOffset * BytesPerElement;
        int sizeBytes = data.Length * BytesPerElement;
        
        fixed (T* dataPtr = data)
        {
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, new(offsetBytes), sizeBytes, new(dataPtr));
        }
    }

    // Caller must bind first.
    public void ClearAndUpload(DynamicArray<T> data)
    {
        Data.Clear();
        Data.AddRange(data);
        
        NeedsUpload = data.Length > 0;
        UploadIfNeeded();
    }

    public void BindBase(int index)
    {
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, index, Name);
    }
}