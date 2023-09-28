using System;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Buffers.Vertices;

public class ElementArrayBuffer : ArrayBufferObject<uint>
{
    public ElementArrayBuffer(string label, UsageHint hint, int capacity = DefaultCapacity) : 
        base(label, hint, capacity)
    {
    }

    public ElementArrayBuffer(string label, BufferUsageHint hint, int capacity = DefaultCapacity) : 
        base(label, hint, capacity)
    {
    }
    
    public void DrawElements(PrimitiveType type = PrimitiveType.Triangles)
    {
        if (Count != 0)
            GL.DrawElements(type, 0, DrawElementsType.UnsignedInt, IntPtr.Zero);
    }
    
    public void DrawElements(PrimitiveType type, int count)
    {
        if (count == 0)
            return;
        
        if (count > m_data.Length)
            throw new($"Trying to draw EBO of size {count} but is out of range");
        
        GL.DrawElements(type, count, DrawElementsType.UnsignedInt, IntPtr.Zero);
    }
}