using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Buffers.Vertices;

public class ArrayBufferObject<T> : BufferObject<T> where T : struct
{
    public ArrayBufferObject(string label, UsageHint hint, int capacity = DefaultCapacity) : 
        this(label, hint.ToBufferUsageHint(), capacity)
    {
    }
    
    public ArrayBufferObject(string label, BufferUsageHint hint, int capacity = DefaultCapacity) : 
        base(label, BufferTarget.ArrayBuffer, hint, capacity)
    {
    }
}