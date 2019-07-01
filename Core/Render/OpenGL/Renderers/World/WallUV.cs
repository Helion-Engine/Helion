namespace Helion.Render.OpenGL.Renderers.World
{
    public struct WallUV
    {
        public readonly float LeftU;
        public readonly float RightU;
        public readonly float TopV;
        public readonly float BottomV;

        public WallUV(float leftU, float rightU, float topV, float bottomV)
        {
            LeftU = leftU;
            RightU = rightU;
            TopV = topV;
            BottomV = bottomV;
        }
    }
}