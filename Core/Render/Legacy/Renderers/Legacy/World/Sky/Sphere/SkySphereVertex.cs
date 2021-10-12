using System.Runtime.InteropServices;

namespace Helion.Render.Legacy.Renderers.Legacy.World.Sky.Sphere;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SkySphereVertex
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;
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
