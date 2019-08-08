using System.Numerics;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Dynamic.Flats
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct DynamicFlatVertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly int PlaneIndex;
        public readonly int TextureTableIndex;

        public DynamicFlatVertex(Vector2 position, int planeIndex, int textureTableIndex)
        {
            X = position.X;
            Y = position.Y;
            PlaneIndex = planeIndex;
            TextureTableIndex = textureTableIndex;
        }
    }
}