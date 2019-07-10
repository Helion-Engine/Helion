using System.Numerics;
using System.Runtime.InteropServices;
using Helion.Util.Geometry;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct StaticWorldVertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float U;
        public readonly float V;
        public readonly float LightLevel;

        public StaticWorldVertex(Vector3 position, Vector2 uv, float lightLevel) :
            this(position.X, position.Y, position.Z, uv.U(), uv.V(), lightLevel)
        {
        }
        
        public StaticWorldVertex(float x, float y, float z, float u, float v, float lightLevel)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
            LightLevel = lightLevel;
        }
    }
}