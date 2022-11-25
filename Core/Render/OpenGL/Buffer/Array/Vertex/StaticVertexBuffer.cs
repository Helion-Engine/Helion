using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Vertex;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Buffer.Array.Vertex;

public class StaticVertexBuffer<T> : VertexBufferObject<T> where T : struct
{
    public StaticVertexBuffer(VertexArrayObject vao, string objectLabel = "") :
        base(vao, objectLabel)
    {
    }

    protected override BufferUsageHint GetBufferUsageType() => BufferUsageHint.StaticDraw;
}
