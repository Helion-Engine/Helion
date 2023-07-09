using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.OpenGL.Buffers;

public class VertexBufferObject<T> : BufferObject<T>, IDisposable where T : struct
{
    public VertexBufferObject(string label, BufferUsageHint hint, int capacity = DefaultCapacity) : 
        base($"[VBO] {label}", BufferTarget.ArrayBuffer, hint, capacity)
    {
    }
    
    public void DrawArrays(PrimitiveType primitiveType)
    {
        if (Data.Length == 0)
            return;

        Debug.Assert(!NeedsUpload, "Forgot to upload VBO data");
        
        GL.DrawArrays(primitiveType, 0, Data.Length);
    }
}