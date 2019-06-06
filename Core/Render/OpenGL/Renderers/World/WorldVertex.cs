namespace Helion.Render.OpenGL.Renderers.World
{
    public readonly struct WorldVertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float U;
        public readonly float V;
        public readonly float Alpha;
        public readonly float UnitLightLevel;

        public WorldVertex(float x, float y, float z, float u, float v, float alpha, float unitLightLevel)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
            Alpha = alpha;
            UnitLightLevel = unitLightLevel;
        }
    }
}
