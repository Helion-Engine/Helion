using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Buffers.Pixels;

public class UnpackPixelBufferObject : BufferObject<uint>
{
    public UnpackPixelBufferObject(string label, BufferUsageHint hint, int capacity = DefaultCapacity) : 
        base(label, BufferTarget.PixelUnpackBuffer, hint, capacity)
    {
    }
}