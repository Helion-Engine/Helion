using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Vertex;

namespace Helion.UI.Shaders.GlowingMap;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct HudVertex
{
    [VertexAttribute(name: "pos")]
    public readonly Vec3F Pos;
    
    [VertexAttribute(name: "uv")]
    public readonly Vec2F UV;

    public HudVertex(Vec3F pos, Vec2F uv)
    {
        Pos = pos;
        UV = uv;
    }
}