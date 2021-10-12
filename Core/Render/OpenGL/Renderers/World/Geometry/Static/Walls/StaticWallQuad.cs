using Helion.Render.Common.World.Triangulation;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static.Walls;

public readonly struct StaticWallQuad
{
    public readonly StaticWallVertex TopLeft;
    public readonly StaticWallVertex TopRight;
    public readonly StaticWallVertex BottomLeft;
    public readonly StaticWallVertex BottomRight;

    public StaticWallQuad(WallTriangulation triangulation)
    {
        // TODO
        TopLeft = default;
        TopRight = default;
        BottomLeft = default;
        BottomRight = default;
    }
}

