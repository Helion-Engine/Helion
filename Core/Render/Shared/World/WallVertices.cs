namespace Helion.Render.Shared.World
{
    public struct WallVertices
    {
        public readonly WorldVertex TopLeft;
        public readonly WorldVertex TopRight;
        public readonly WorldVertex BottomLeft;
        public readonly WorldVertex BottomRight;

        public WallVertices(WorldVertex topLeft, WorldVertex topRight, WorldVertex bottomLeft, WorldVertex bottomRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }
    }
}