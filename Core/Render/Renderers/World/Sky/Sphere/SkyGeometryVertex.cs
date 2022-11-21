using System.Runtime.InteropServices;
using Helion;
using Helion.Render;
using Helion.Render.Common.Shared.World;
using Helion.Render.Renderers.World.Sky.Sphere;

namespace Helion.Render.Renderers.World.Sky.Sphere;

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

    public SkyGeometryVertex(TriangulatedVertex vertex) : this(vertex.X, vertex.Y, vertex.Z)
    {
    }
}
