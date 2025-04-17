using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EntityVertex
{    
    [VertexAttribute]
    public Vec3F Pos;

    [VertexAttribute]
    // Health Percent Normalized when in health bar mode
    public float LightLevel;

    [VertexAttribute]
    // X offset written to colormap option portion when in health bar mode
    public float Options;

    [VertexAttribute]
    public Vec3F PrevPos;

    [VertexAttribute]
    public float OffsetZ;

    [VertexAttribute]
    public Vec3F CenterPos;

    [VertexAttribute(required: false)]
    public float SectorIndex;
}
