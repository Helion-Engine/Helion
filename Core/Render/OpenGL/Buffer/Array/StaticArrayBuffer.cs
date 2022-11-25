using Helion.Render.OpenGL.Context;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Buffer.Array;

public class StaticArrayBuffer<T> : ArrayBufferObject<T> where T : struct
{
    public StaticArrayBuffer(string objectLabel = "") :
        base(objectLabel)
    {
    }

    protected override BufferUsageHint GetBufferUsageType() => BufferUsageHint.StaticDraw;
}
