using Helion.Render.OpenGL.Legacy.Context;
using Helion.Render.OpenGL.Legacy.Context.Types;
using Helion.Render.OpenGL.Legacy.Vertex;

namespace Helion.Render.OpenGL.Legacy.Buffer.Array.Vertex
{
    public class StreamVertexBuffer<T> : VertexBufferObject<T> where T : struct
    {
        public StreamVertexBuffer(GLCapabilities capabilities, IGLFunctions functions, VertexArrayObject vao, string objectLabel = "") : 
            base(capabilities, functions, vao, objectLabel)
        {
        }

        protected override BufferUsageType GetBufferUsageType() => BufferUsageType.StreamDraw;
    }
}