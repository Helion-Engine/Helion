using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Primitives;

public readonly struct PrimitiveVertex
{
    [VertexAttribute]
    public readonly Vec3F Pos;

    [VertexAttribute]
    public readonly Vec3F Rgb;

    [VertexAttribute]
    public readonly float Alpha;

    public PrimitiveVertex(Vec3F pos, Vec3F rgb, float alpha)
    {
        Pos = pos;
        Rgb = rgb;
        Alpha = alpha;
    }
}
