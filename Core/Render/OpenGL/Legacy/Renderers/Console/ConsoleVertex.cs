namespace Helion.Render.OpenGL.Legacy.Renderers.Console
{
    public struct ConsoleVertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly float U;
        public readonly float V;

        public ConsoleVertex(float x, float y, float u, float v)
        {
            X = x;
            Y = y;
            U = u;
            V = v;
        }
    }
}
