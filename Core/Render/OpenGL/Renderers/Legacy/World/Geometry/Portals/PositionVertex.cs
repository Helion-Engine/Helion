using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Vertex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals;

/// <summary>
/// Intended to be a trivial vertex that is used only for being turned into
/// fragments for stencil writing.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct PositionVertex
{
    [VertexAttribute]
    public readonly Vec3F Pos;

    public PositionVertex(Vec3F pos)
    {
        Pos = pos;
    }

    public PositionVertex(float x, float y, float z) : this((x, y, z))
    {
    }
}
