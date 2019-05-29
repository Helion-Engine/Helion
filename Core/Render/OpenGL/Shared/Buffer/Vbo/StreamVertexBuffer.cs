using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Shared.Buffer.Vbo
{
    public class StreamVertexBuffer<T> : VertexBuffer<T> where T : struct
    {
        protected override BufferUsageHint GetHint() => BufferUsageHint.StreamDraw;

        protected override void Unbind()
        {
            base.Unbind();
            Clear();
        }
    }
}
