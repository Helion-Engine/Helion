using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;

namespace Helion.Render.OpenGL.Textures.Buffer.Data
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public readonly struct FrameData
    {
        public readonly float Flags;
        public readonly Vec2F Offset;
        public readonly float Unused3; // Required to align to a power of two and for texels.
        public readonly Vec4F RotationTextureIndex1234;
        public readonly Vec4F RotationTextureIndex5678;

        public FrameData(int flags, Vec2F offset, int frameTexIndex1, int frameTexIndex2, int frameTexIndex3, 
            int frameTexIndex4, int frameTexIndex5, int frameTexIndex6, int frameTexIndex7, int frameTexIndex8)
        {
            Flags = flags;
            Offset = offset;
            Unused3 = 0;
            RotationTextureIndex1234 = (frameTexIndex1, frameTexIndex2, frameTexIndex3, frameTexIndex4);
            RotationTextureIndex5678 = (frameTexIndex5, frameTexIndex6, frameTexIndex7, frameTexIndex8);
        }
    }
}
