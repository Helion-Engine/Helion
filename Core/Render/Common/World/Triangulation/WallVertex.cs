using Helion.Geometry.Vectors;

namespace Helion.Render.Common.World.Triangulation;

public readonly struct WallVertex
{
    public readonly Vec3F Pos;
    public readonly Vec2F UV;

    public WallVertex(Vec3F pos, Vec2F uv)
    {
        Pos = pos;
        UV = uv;
    }
}
