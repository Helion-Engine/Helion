using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Primitives;

namespace Helion.Render.OpenGL.Renderers.World.Primitives;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct PrimitiveWorldVertex
{
    public readonly Vec3F Pos;
    public readonly ByteColor Color;

    public PrimitiveWorldVertex(Vec3F pos, ByteColor color)
    {
        Pos = pos;
        Color = color;
    }
}

