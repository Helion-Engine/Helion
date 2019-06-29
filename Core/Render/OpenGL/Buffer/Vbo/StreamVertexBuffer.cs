using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Buffer.Vbo
{
    public class StreamVertexBuffer<T> : VertexBuffer<T> where T : struct
    {
        protected override BufferUsageHint GetHint() => BufferUsageHint.StreamDraw;

        public override void Unbind()
        {
            base.Unbind();
            Clear();
        }
    }
}
