using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Shared.World;

public struct WallVertices
{
    public TriangulatedWorldVertex TopLeft;
    public TriangulatedWorldVertex TopRight;
    public TriangulatedWorldVertex BottomLeft;
    public TriangulatedWorldVertex BottomRight;
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
