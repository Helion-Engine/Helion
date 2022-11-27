using Helion.Render.OpenGL.Vertex;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LegacyVertex
{
    [VertexAttribute("pos", size: 3)]
    public float X;
    public float Y;
    public float Z;

    [VertexAttribute("uv", size: 2)]
    public float U;
    public float V;

    [VertexAttribute]
    public float LightLevel;

    [VertexAttribute]
    public float Alpha;

    [VertexAttribute("colorMul", size: 3)]
    public float R;
    public float G;
    public float B;

    [VertexAttribute]
    public float Fuzz;

    public LegacyVertex(float x, float y, float z, float u, float v, short lightLevel = 256, float alpha = 1.0f, float fuzz = 0.0f)
    {
        X = x;
        Y = y;
        Z = z;
        U = u;
        V = v;
        LightLevel = lightLevel;
        Alpha = alpha;
        R = 1f;
        G = 1f;
        B = 1f;
        Fuzz = fuzz;
    }
}
