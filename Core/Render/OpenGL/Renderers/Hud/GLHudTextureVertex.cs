using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Attributes;
using Helion.Render.OpenGL.Primitives;

namespace Helion.Render.OpenGL.Renderers.Hud;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct GLHudTextureVertex
{
    public readonly Vec3F Pos;
    public readonly Vec2F UV;
    [Normalized]
    public readonly ByteColor ScaleRgba;
    public readonly float Alpha;

    public GLHudTextureVertex(Vec3F pos, Vec2F uv, ByteColor scaleRgba, float alpha)
    {
        Pos = pos;
        UV = uv;
        ScaleRgba = scaleRgba;
        Alpha = alpha;
    }
}

