using System.Numerics;
using System.Runtime.InteropServices;

namespace Helion.Render.OldOpenGL.Renderers.World.Geometry.Dynamic.Walls
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct DynamicWallVertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly int UpperPlaneIndex;
        public readonly int LowerPlaneIndex;
        public readonly int SectorCeilingPlaneIndex;
        public readonly int TextureTableIndex;
        public readonly int Flags;
        public readonly float OffsetX;
        public readonly float OffsetY;

        public DynamicWallVertex(Vector2 position, int upperPlaneIndex, int lowerPlaneIndex, 
            int sectorCeilingPlaneIndex, int textureTableIndex, int flags, Vector2 offset)
        {
            X = position.X;
            Y = position.Y;
            UpperPlaneIndex = upperPlaneIndex;
            LowerPlaneIndex = lowerPlaneIndex;
            SectorCeilingPlaneIndex = sectorCeilingPlaneIndex;
            TextureTableIndex = textureTableIndex;
            Flags = flags;
            OffsetX = offset.X;
            OffsetY = offset.Y;
        }
    }
}