using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Vertex;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FloodFillPlaneVertex
{
    [VertexAttribute]
    public readonly Vec2F Pos;

    [VertexAttribute]
    public readonly Vec2F UV;

    public FloodFillPlaneVertex(Vec2F pos, Vec2F uv)
    {
        Pos = pos;
        UV = uv;
    }
}