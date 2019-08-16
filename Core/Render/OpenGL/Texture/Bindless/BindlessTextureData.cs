using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Texture.Bindless
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct BindlessTextureData
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int OffsetX;
        public readonly int OffsetY;
        public readonly float InverseU;
        public readonly float InverseV;
        public readonly ulong BindlessHandle;

        public BindlessTextureData(int width, int height, int offsetX, int offsetY, float inverseU, float inverseV, 
            ulong bindlessHandle)
        {
            Width = width;
            Height = height;
            OffsetX = offsetX;
            OffsetY = offsetY;
            InverseU = inverseU;
            InverseV = inverseV;
            BindlessHandle = bindlessHandle;
        }
    }
}