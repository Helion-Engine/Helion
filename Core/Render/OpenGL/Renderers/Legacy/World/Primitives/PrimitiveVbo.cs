using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Primitives;

class PrimitiveVbo
{
    public PrimitiveVbo(string name, int lineWidth)
    {
        Vbo = new(name);
        Vao = new(name);
        LineWidth = lineWidth;
    }

    public readonly StreamVertexBuffer<PrimitiveVertex> Vbo;
    public readonly VertexArrayObject Vao;
    public readonly int LineWidth;
}