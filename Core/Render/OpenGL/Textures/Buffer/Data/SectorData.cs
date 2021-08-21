using System.Drawing;
using System.Runtime.InteropServices;
using Helion.Geometry.Planes;
using Helion.Geometry.Vectors;

namespace Helion.Render.OpenGL.Textures.Buffer.Data
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public readonly struct SectorData
    {
        public readonly Vec4F StartPlane;
        public readonly Vec4F EndPlane;
        public readonly Vec4F Color;
        public readonly float TextureIndex;
        public readonly float LightLevel;
        public readonly float Unused2;
        public readonly float Unused3;

        public SectorData(Plane3D start, Plane3D end, Color color, int textureIndex, byte lightLevel)
        {
            StartPlane = start.Vec;
            EndPlane = end.Vec;
            Color = (color.R, color.G, color.B, color.A);
            TextureIndex = textureIndex;
            LightLevel = lightLevel / 255.0f;
            Unused2 = 0;
            Unused3 = 0;
        }
    }
}
