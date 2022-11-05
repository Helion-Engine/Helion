using System.Runtime.InteropServices;

namespace Helion.Render.Legacy.Renderers.Legacy.World.Geometry.Static;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StaticGeometryVertex
{
    public float X;
    public float Y;
    public float Z;
    public float U;
    public float V;
    public float LightLevel;

    public StaticGeometryVertex(float x, float y, float z, float u, float v, short lightLevel)
    {
        X = x;
        Y = y;
        Z = z;
        U = u;
        V = v;
        LightLevel = lightLevel;
    }
}
