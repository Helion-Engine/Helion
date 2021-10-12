using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Attributes;
using Helion.Render.OpenGL.Primitives;

namespace Helion.Render.OpenGL.Renderers.Hud;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct GLHudPrimitiveVertex
{
    public readonly Vec3F Pos;
    [Normalized]
    public readonly ByteColor Color;

    public GLHudPrimitiveVertex(Vec3F pos, ByteColor color)
    {
        Pos = pos;
        Color = color;
    }
}
