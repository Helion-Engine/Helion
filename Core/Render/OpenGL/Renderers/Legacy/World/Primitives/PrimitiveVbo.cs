using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Primitives;

sealed class PrimitiveVbo
{
    public PrimitiveVbo(IRenderProgramHandle program, string name, int lineWidth)
    {
        Pipeline = new(program, new StreamVertexBuffer<PrimitiveVertex>("Primitive"), "Primitive");
        LineWidth = lineWidth;
    }

    public readonly VertexPipeline<PrimitiveVertex> Pipeline;
    public readonly int LineWidth;
}