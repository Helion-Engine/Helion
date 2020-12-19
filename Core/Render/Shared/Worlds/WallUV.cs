using System.Numerics;

namespace Helion.Render.Shared.Worlds
{
    public struct WallUV
    {
        public readonly Vector2 TopLeft;
        public readonly Vector2 BottomRight;

        public WallUV(Vector2 topLeft, Vector2 bottomRight)
        {
            TopLeft = topLeft;
            BottomRight = bottomRight;
        }
    }
}