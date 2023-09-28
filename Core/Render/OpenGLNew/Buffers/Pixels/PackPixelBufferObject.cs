using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Buffers.Pixels;

public class PackPixelBufferObject : BufferObject<uint>
{
    public PackPixelBufferObject(string label, BufferUsageHint hint, int capacity = DefaultCapacity) : 
        base(label, BufferTarget.PixelPackBuffer, hint, capacity)
    {
    }
}