using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Texture
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct TextureInfo
    {
        public readonly float LeftU;
        public readonly float BottomV;
        public readonly float RightU;
        public readonly float TopV;
        public readonly float InverseU;
        public readonly float InverseV;
        public readonly int Width;
        public readonly int Height;

        public TextureInfo(GLTexture texture)
        {
            LeftU = texture.UVLocation.Min.X;
            BottomV = texture.UVLocation.Min.Y;
            RightU = texture.UVLocation.Max.X;
            TopV = texture.UVLocation.Max.Y;
            InverseU = texture.UVInverse.X;
            InverseV = texture.UVInverse.Y;
            Width = texture.Dimension.Width;
            Height = texture.Dimension.Height;
        }
    }
}