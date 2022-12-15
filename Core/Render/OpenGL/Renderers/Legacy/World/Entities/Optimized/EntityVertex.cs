using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Vertex;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities.Optimized;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct EntityVertex
{
    [VertexAttribute]
    public readonly Vec3F Pos;

    [VertexAttribute(normalized: true)]
    public readonly byte LightLevel;

    public EntityVertex(Vec3F pos, byte lightLevel)
    {
        Pos = pos;
        LightLevel = lightLevel;
    }
}
