using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static.Walls;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct StaticWallVertex
{
    public readonly Vec3F Pos;
    public readonly Vec2F UV;

    public StaticWallVertex(Vec3F pos, Vec2F uv)
    {
        Pos = pos;
        UV = uv;
    }
}

