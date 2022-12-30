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
    
    [VertexAttribute("prevPos", size: 3)]
    public float PrevX;
    public float PrevY;
    public float PrevZ;

    [VertexAttribute("uv", size: 2)]
    public float U;
    public float V;

    [VertexAttribute]
    public float LightLevel;

    [VertexAttribute]
    public float Alpha;

    [VertexAttribute]
    public float Fuzz;

    public LegacyVertex(float x, float y, float z, float u, float v, short lightLevel = 256, float alpha = 1.0f, float fuzz = 0.0f)
    {
        X = x;
        Y = y;
        Z = z;
        PrevX = x;
        PrevY = y;
        PrevZ = z;
        U = u;
        V = v;
        //PrevU = u;
        //PrevV = v;
        LightLevel = lightLevel;
        Alpha = alpha;
        Fuzz = fuzz;        
    }

    public LegacyVertex(float x, float y, float z, float prevX, float prevY, float prevZ, float u, float v, short lightLevel = 256, float alpha = 1.0f, float fuzz = 0.0f)
    {
        X = x;
        Y = y;
        Z = z;
        PrevX = prevX;
        PrevY = prevY;
        PrevZ = prevZ;
        U = u;
        V = v;
        //PrevU = u;
        //PrevV = v;
        LightLevel = lightLevel;
        Alpha = alpha;
        Fuzz = fuzz;
    }
}
