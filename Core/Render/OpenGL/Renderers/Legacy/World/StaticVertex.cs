using Helion.Render.OpenGL.Vertex;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StaticVertex(float x, float y, float z, float u, float v, float alpha, float addAlpha, float lightLevelBufferIndex, float lightLevelAdd)
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
    public float Options = alpha + (addAlpha * 2) + (lightLevelBufferIndex * 4);
}
