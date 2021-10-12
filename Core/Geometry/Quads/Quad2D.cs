using Helion.Geometry.Vectors;

namespace Helion.Geometry.Quads;

public readonly struct Quad2D
{
    public readonly Vec2D TopLeft;
    public readonly Vec2D TopRight;
    public readonly Vec2D BottomLeft;
    public readonly Vec2D BottomRight;

    public Quad2D(Vec2D topLeft, Vec2D topRight, Vec2D bottomLeft, Vec2D bottomRight)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomLeft = bottomLeft;
        BottomRight = bottomRight;
    }
}
