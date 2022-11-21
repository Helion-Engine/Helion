using Helion;
using Helion.Render;
using Helion.Render.Common.Shared.World;

namespace Helion.Render.Common.Shared.World;

public struct WallVertices
{
    public readonly TriangulatedVertex TopLeft;
    public readonly TriangulatedVertex TopRight;
    public readonly TriangulatedVertex BottomLeft;
    public readonly TriangulatedVertex BottomRight;

    public WallVertices(in TriangulatedVertex topLeft, in TriangulatedVertex topRight, in TriangulatedVertex bottomLeft, in TriangulatedVertex bottomRight)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomLeft = bottomLeft;
        BottomRight = bottomRight;
    }
}
