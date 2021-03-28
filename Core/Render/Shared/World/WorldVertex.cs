using Helion.Geometry.Vectors;

namespace Helion.Render.Shared.World
{
    public struct WorldVertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float U;
        public readonly float V;

        public WorldVertex(float x, float y, float z, float u, float v)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
        }

        public WorldVertex(double x, double y, double z, double u, double v) :
            this((float)x, (float)y, (float)z, (float)u, (float)v)
        {
        }

        public WorldVertex(Vec3F position, Vec2F uv) :
            this(position.X, position.Y, position.Z, uv.X, uv.Y)
        {
        }

        public override string ToString() => $"{X}, {Y}, {Z} [{U}, {V}]";
    }
}