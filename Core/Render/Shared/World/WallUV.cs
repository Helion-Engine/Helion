using Helion.Geometry.Vectors;

namespace Helion.Render.Shared.World
{
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
}