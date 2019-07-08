using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct StaticWorldVertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly float U;
        public readonly int FloorPlaneIndex;
        public readonly int CeilingPlaneIndex;
        public readonly int WallIndex;
        public readonly int Flags;

        public StaticWorldVertex(float x, float y, float u, int floorPlaneIndex, int ceilingPlaneIndex, 
            int wallIndex, int flags)
        {
            X = x;
            Y = y;
            U = u;
            FloorPlaneIndex = floorPlaneIndex;
            CeilingPlaneIndex = ceilingPlaneIndex;
            WallIndex = wallIndex;
            Flags = flags;
        }
    }
}