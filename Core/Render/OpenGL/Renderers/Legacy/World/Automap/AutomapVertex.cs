using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Vertex;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Automap;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct AutomapVertex
{
    [VertexAttribute]
    public readonly Vec2F Pos;

    public AutomapVertex(Vec2F pos)
    {
        Pos = pos;
    }

    public AutomapVertex(float x, float y) : this((x, y))
    {
    }
}
