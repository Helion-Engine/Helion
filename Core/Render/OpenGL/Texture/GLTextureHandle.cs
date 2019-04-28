using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Texture
{
    /// <summary>
    /// A texture handle that is packed tightly to place on the GPU.
    /// </summary>
    /// <remarks>
    /// This is intended for GPUs that support the bindless texture extension.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class GLTextureHandle
    {
        /// <summary>
        /// The resident handle for bindless texture samplers to use.
        /// </summary>
        public readonly long ResidentHandle;

        /// <summary>
        /// How many pixels wide the texture is.
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// How many pixels tall the texture is.
        /// </summary>
        public readonly int Height;

        public GLTextureHandle(long residentHandle, int width, int height)
        {
            ResidentHandle = residentHandle;
            Width = width;
            Height = height;
        }
    }
}
