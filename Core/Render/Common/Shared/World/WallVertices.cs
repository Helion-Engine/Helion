namespace Helion.Render.OpenGL.Shared.World;

public struct WallVertices
{
    public TriangulatedWorldVertex TopLeft;
    public TriangulatedWorldVertex TopRight;
    public TriangulatedWorldVertex BottomLeft;
    public TriangulatedWorldVertex BottomRight;
    public float PrevTopZ;
    public float PrevBottomZ;
}
