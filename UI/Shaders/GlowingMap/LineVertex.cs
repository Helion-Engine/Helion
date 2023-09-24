using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Vertex;

namespace Helion.UI.Shaders.GlowingMap;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct LineVertex
{
    [VertexAttribute(name: "pos")]
    public readonly Vec3F Pos;
    
    [VertexAttribute(name: "numSides", isIntegral: true)]
    public readonly int NumSides;
    
    [VertexAttribute(name: "rootDistance")]
    public readonly float RootDistance;

    public LineVertex(Vec3F pos, bool oneSided, int rootDistance)
    {
        Pos = pos;
        NumSides = oneSided ? 1 : 2;
        RootDistance = rootDistance;
    }
}