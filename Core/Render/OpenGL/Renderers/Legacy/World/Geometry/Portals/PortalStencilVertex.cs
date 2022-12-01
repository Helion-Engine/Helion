using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Vertex;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals;

/// <summary>
/// Intended to be a trivial vertex that is used only for being turned into
/// fragments for stencil writing.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct PortalStencilVertex
{
    [VertexAttribute]
    public readonly Vec3F Pos;

    public PortalStencilVertex(Vec3F pos)
    {
        Pos = pos;
    }

    public PortalStencilVertex(float x, float y, float z) : this((x, y, z))
    {
    }
}
