using Helion.Render.OpenGL.Vertex;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StaticVertex(float x, float y, float z, float u, float v, float options, float lightLevelAdd, float colorMapIndex)
{
    [VertexAttribute("pos", size: 3)]
    public float X = x;
    public float Y = y;
    public float Z = z;

    [VertexAttribute("uv", size: 2)]
    public float U = u;
    public float V = v;

    [VertexAttribute]
    public float LightLevelAdd = lightLevelAdd;

    [VertexAttribute]
    public float Options = options;

    [VertexAttribute(required: false)]
    public float ColoMapIndex = colorMapIndex;
}
