namespace Helion.Render.OpenGL.Modern.Texture
{
    /// <summary>
    /// Information for a texture that we create.
    /// </summary>
    public class GLTexture
    {
        /// <summary>
        /// The OpenGL 'name' which is the texture ID when generating textures.
        /// </summary>
        public readonly int Name;

        /// <summary>
        /// The offset into the texture handle buffer array on the GPU.
        /// </summary>
        public readonly int BufferOffset;

        /// <summary>
        /// The handle information for this texture.
        /// </summary>
        public readonly GLTextureHandle Handle;

        public GLTexture(int name, int bufferOffset, GLTextureHandle handle)
        {
            Name = name;
            BufferOffset = bufferOffset;
            Handle = handle;
        }
    }
}
