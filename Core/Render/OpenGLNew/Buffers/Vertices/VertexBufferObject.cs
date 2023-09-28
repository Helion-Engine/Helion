using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Buffers.Vertices;

public class VertexBufferObject<T> : ArrayBufferObject<T> where T : struct
{
    public VertexBufferObject(string label, UsageHint hint, int capacity = DefaultCapacity) : 
        base(label, hint, capacity)
    {
    }
    
    public void DrawArrays(PrimitiveType type = PrimitiveType.Triangles)
    {
        if (Count != 0)
            GL.DrawArrays(type, 0, Count);
    }
    
    public void DrawArrays(PrimitiveType type, int first, int count)
    {
        if (count == 0)
            return;
        
        if (first + count > m_data.Length)
            throw new($"Trying to draw VBO span from {first} to {first + count} but is out of range");
        
        GL.DrawArrays(type, first, count);
    }
}