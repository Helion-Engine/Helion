namespace Helion.Render.OpenGL.Legacy.Renderers.World
{
    public readonly struct WorldVertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float U;
        public readonly float V;
        public readonly byte Brightness;

        public WorldVertex(float x, float y, float z, float u, float v, byte brightness)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
            Brightness = brightness;
        }
    }
}
