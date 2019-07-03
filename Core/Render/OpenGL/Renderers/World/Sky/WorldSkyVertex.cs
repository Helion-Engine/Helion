namespace Helion.Render.OpenGL.Renderers.World.Sky
{
    public struct WorldSkyStencilVertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public WorldSkyStencilVertex(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public WorldSkyStencilVertex(WorldVertex worldVertex)
        {
            X = worldVertex.X;
            Y = worldVertex.Y;
            Z = worldVertex.Z;
        }
    }
    
    public struct WorldSkyVertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float U;
        public readonly float V;

        public WorldSkyVertex(float x, float y, float z, float u, float v)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
        }
    }
}