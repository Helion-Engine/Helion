using Helion.Render.OpenGL.Buffer.Array.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Primitives;

sealed class PrimitiveVbo
{
    public PrimitiveVbo(PrimitiveShader program, string name, int lineWidth)
    {
        Pipeline = new(program, new StreamVertexBuffer<PrimitiveVertex>("Primitive"), "Primitive");
        LineWidth = lineWidth;
    }

    public readonly VertexPipeline<PrimitiveVertex> Pipeline;
    public readonly int LineWidth;
}