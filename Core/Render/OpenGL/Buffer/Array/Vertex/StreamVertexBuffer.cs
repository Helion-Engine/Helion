using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Buffer.Array.Vertex
{
    public class StreamVertexBuffer<T> : VertexBufferObject<T> where T : struct
    {
        public StreamVertexBuffer(GLCapabilities capabilities, IGLFunctions functions, VertexArrayObject vao, string objectLabel = "") : 
            base(capabilities, functions, vao, objectLabel)
        {
        }

        public override void DrawArrays()
        {
            base.DrawArrays();
            Clear();
        }

        protected override BufferUsageType GetBufferUsageType() => BufferUsageType.StreamDraw;
    }
}