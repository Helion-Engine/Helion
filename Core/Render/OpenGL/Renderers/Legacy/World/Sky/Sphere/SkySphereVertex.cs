using Helion.Render.OpenGL.Vertex;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SkySphereVertex
{
    [VertexAttribute("pos", size: 3)]
    public readonly float X;
    public readonly float Y;
    public readonly float Z;

    [VertexAttribute("uv", size: 2)]
    public readonly float U;
    public readonly float V;

    public SkySphereVertex(float x, float y, float z, float u, float v)
    {
        X = x;
        Y = y;
        Z = z;
        U = u;
        V = v;
    }
}
