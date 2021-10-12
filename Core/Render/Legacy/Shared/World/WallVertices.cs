namespace Helion.Render.Legacy.Shared.World;

public struct WallVertices
{
    public readonly WorldVertex TopLeft;
    public readonly WorldVertex TopRight;
    public readonly WorldVertex BottomLeft;
    public readonly WorldVertex BottomRight;

    public WallVertices(in WorldVertex topLeft, in WorldVertex topRight, in WorldVertex bottomLeft, in WorldVertex bottomRight)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomLeft = bottomLeft;
        BottomRight = bottomRight;
    }
}
