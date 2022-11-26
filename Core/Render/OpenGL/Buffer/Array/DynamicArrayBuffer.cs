using Helion.Render.OpenGL.Context;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Buffer.Array;

public class DynamicArrayBuffer<T> : ArrayBufferObject<T> where T : struct
{
    public DynamicArrayBuffer(string objectLabel) : base(objectLabel)
    {
    }

    protected override BufferUsageHint GetBufferUsageType() => BufferUsageHint.DynamicDraw;
}
