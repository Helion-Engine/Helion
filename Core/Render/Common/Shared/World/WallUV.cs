using Helion;
using Helion.Geometry.Vectors;
using Helion.Render;
using Helion.Render.Common.Shared.World;

namespace Helion.Render.Common.Shared.World;

public struct WallUV
{
    public readonly Vec2F TopLeft;
    public readonly Vec2F BottomRight;

    public WallUV(Vec2F topLeft, Vec2F bottomRight)
    {
        TopLeft = topLeft;
        BottomRight = bottomRight;
    }
}
