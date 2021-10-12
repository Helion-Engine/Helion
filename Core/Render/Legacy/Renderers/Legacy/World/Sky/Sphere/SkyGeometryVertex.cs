using System.Runtime.InteropServices;
using Helion.Render.Legacy.Shared.World;

namespace Helion.Render.Legacy.Renderers.Legacy.World.Sky.Sphere;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SkyGeometryVertex
{
    public float X;
    public float Y;
    public float Z;

    public SkyGeometryVertex(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public SkyGeometryVertex(WorldVertex vertex) : this(vertex.X, vertex.Y, vertex.Z)
    {
    }
}

