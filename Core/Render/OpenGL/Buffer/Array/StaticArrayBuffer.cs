using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;

namespace Helion.Render.OpenGL.Buffer.Array;

public class StaticArrayBuffer<T> : ArrayBufferObject<T> where T : struct
{
    public StaticArrayBuffer(GLCapabilities capabilities, IGLFunctions functions, string objectLabel = "") :
        base(capabilities, functions, objectLabel)
    {
    }

    protected override BufferUsageType GetBufferUsageType() => BufferUsageType.StaticDraw;
}
