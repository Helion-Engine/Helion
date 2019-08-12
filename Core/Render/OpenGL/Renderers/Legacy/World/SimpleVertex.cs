using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SimpleVertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float U;
        public readonly float V;

        public SimpleVertex(float x, float y, float z, float u, float v)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
        }
    }
}