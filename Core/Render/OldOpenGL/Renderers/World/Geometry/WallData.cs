using System.Runtime.InteropServices;

namespace Helion.Render.OldOpenGL.Renderers.World.Geometry
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct WallData
    {
        public readonly float LightLevel;
        public readonly float OffsetX;
        public readonly float OffsetY;
        public readonly float Flags;
        public readonly float TextureTableIndex;

        public WallData(float lightLevel, float offsetX, float offsetY, float flags, float textureTableIndex)
        {
            LightLevel = lightLevel;
            OffsetX = offsetX;
            OffsetY = offsetY;
            Flags = flags;
            TextureTableIndex = textureTableIndex;
        }
    }
}