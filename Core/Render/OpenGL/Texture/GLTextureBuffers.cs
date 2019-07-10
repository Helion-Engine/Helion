using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Util;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Texture
{
    /// <summary>
    /// A collection of all the texture buffer objects that we expose in the
    /// shaders.
    /// </summary>
    public class GLTextureBuffers
    {
        /// <summary>
        /// Contains texture location information so UV coordinates and other
        /// related information can be looked up.
        /// </summary>
        public readonly TextureBufferObject<TextureInfo> TextureInfoBuffer;

        /// <summary>
        /// Creates all the buffers that contain texture data to be referenced
        /// in the shader.
        /// </summary>
        /// <param name="capabilities">The GL capabilities.</param>
        public GLTextureBuffers(GLCapabilities capabilities)
        {
            TextureInfoBuffer = new TextureBufferObject<TextureInfo>(capabilities);
        }

        /// <summary>
        /// Tracks a texture so it becomes available in the shaders.
        /// </summary>
        /// <param name="texture">The texture to make available in the shaders.
        /// </param>
        public void Track(GLTexture texture)
        {
            if (texture.LookupIndex < TextureInfoBuffer.Count)
                Fail("TODO: GLTextureBuffers.Track() not implemented for overwriting insertion");
            else if (texture.LookupIndex == TextureInfoBuffer.Count)
                TextureInfoBuffer.Add(new TextureInfo(texture));
            else
                Fail("Adding GL texture to texture info buffer out of order.");
        }
    }
}