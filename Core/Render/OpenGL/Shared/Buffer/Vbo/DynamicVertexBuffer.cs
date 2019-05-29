using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Shared.Buffer.Vbo
{
    public class DynamicVertexBuffer<T> : VertexBuffer<T> where T : struct
    {
        protected override BufferUsageHint GetHint() => BufferUsageHint.DynamicDraw;
    }
}
