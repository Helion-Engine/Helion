using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.OpenGL.Buffers;

public class ElementBufferObject : BufferObject<uint>
{
    public ElementBufferObject(string label, BufferUsageHint hint, int capacity) : 
        base(label, BufferTarget.ElementArrayBuffer, hint, capacity)
    {
    }
    
    public void DrawElements(PrimitiveType primitiveType)
    {
        if (Data.Length == 0)
            return;

        Debug.Assert(!NeedsUpload, "Forgot to upload EBO data");

        GL.DrawElements(primitiveType, Data.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);
    }
}