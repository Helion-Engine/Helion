namespace Helion.Render.OpenGL.Shared.World;

public struct WallVertices
{
    public readonly TriangulatedWorldVertex TopLeft;
    public readonly TriangulatedWorldVertex TopRight;
    public readonly TriangulatedWorldVertex BottomLeft;
    public readonly TriangulatedWorldVertex BottomRight;
    public float PrevTopZ;
    public float PrevBottomZ;

    public WallVertices(in TriangulatedWorldVertex topLeft, in TriangulatedWorldVertex topRight, in TriangulatedWorldVertex bottomLeft, in TriangulatedWorldVertex bottomRight,
        double topPrevZ, double prevBottomZ)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomLeft = bottomLeft;
        BottomRight = bottomRight;
        PrevTopZ = (float)topPrevZ;
        PrevBottomZ = (float)prevBottomZ;
    }
}
