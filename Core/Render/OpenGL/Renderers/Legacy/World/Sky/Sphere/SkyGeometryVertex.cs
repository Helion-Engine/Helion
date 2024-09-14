using System.Runtime.InteropServices;
using Helion.Render.OpenGL.Shared.World;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SkyGeometryVertex
{
    [VertexAttribute("pos", size: 3)]
    public float X;
    public float Y;
    public float Z;

    [VertexAttribute("prevZ", required: false)]
    public float PrevZ;

    public SkyGeometryVertex(float x, float y, float z, float prevZ)
    {
        X = x;
        Y = y;
        Z = z;
        PrevZ = prevZ;
    }

    public SkyGeometryVertex(TriangulatedWorldVertex vertex) : this(vertex.X, vertex.Y, vertex.Z, vertex.Z)
    {
    }
}
