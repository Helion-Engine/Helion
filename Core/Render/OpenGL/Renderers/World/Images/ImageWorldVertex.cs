using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Primitives;

namespace Helion.Render.OpenGL.Renderers.World.Images;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ImageWorldVertex
{
    public readonly Vec3F Pos;
    public readonly Vec2F UV;
    public readonly ByteColor Color;

    public ImageWorldVertex(Vec3F pos, Vec2F uv, ByteColor color)
    {
        Pos = pos;
        UV = uv;
        Color = color;
    }
}
