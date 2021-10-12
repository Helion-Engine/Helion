using Helion.World.Geometry.Walls;

namespace Helion.Render.Common.World.Triangulation;

public readonly struct WallTriangulation
{
    public readonly WallVertex TopLeft;
    public readonly WallVertex TopRight;
    public readonly WallVertex BottomLeft;
    public readonly WallVertex BottomRight;

    public WallTriangulation(WallVertex topLeft, WallVertex topRight, WallVertex bottomLeft, WallVertex bottomRight)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomLeft = bottomLeft;
        BottomRight = bottomRight;
    }

    public static WallTriangulation From(Wall wall)
    {
        // TODO
        return default;
    }
}
