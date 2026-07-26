using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EntityVertex
{    
    [VertexAttribute(divisor: 1)]
    public Vec3F Pos;

    [VertexAttribute(divisor: 1)]
    // X offset written to surface option portions when in health bar mode
    public float SurfaceOptions;

    [VertexAttribute(divisor: 1)]
    public Vec3F PrevPos;

    [VertexAttribute(divisor: 1)]
    public float OffsetXYZ;

    [VertexAttribute(divisor: 1)]
    public float RenderOptions;
}
