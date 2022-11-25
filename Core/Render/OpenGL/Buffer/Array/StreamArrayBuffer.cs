using Helion.Render.OpenGL.Context;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Buffer.Array;

public class StreamArrayBuffer<T> : ArrayBufferObject<T> where T : struct
{
    public StreamArrayBuffer(string objectLabel = "") :
        base(objectLabel)
    {
    }

    protected override BufferUsageHint GetBufferUsageType() => BufferUsageHint.StreamDraw;
}
