using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Render.Common;

namespace Helion.Render.OpenGL.Textures
{
    /// <summary>
    /// A handle to a texture. Such a handle may either be the entire texture,
    /// or is a component of a larger texture (such as in a texture atlas).
    /// </summary>
    public class GLTextureHandle : IRenderableTexture
    {
        /// <summary>
        /// This is the lookup index where the texture will get its data from
        /// in the shader.
        /// </summary>
        public readonly int Index;
        
        /// <summary>
        /// The texture this is sourced from.
        /// </summary>
        public readonly GLTexture Texture;
        
        /// <summary>
        /// The UV of the texture in question with respect to the 2D plane it
        /// is part of. For three dimensional textures, the depth is not part
        /// of this.
        /// </summary>
        public readonly Box2F UV;
        
        /// <summary>
        /// The dimension of the image in pixels.
        /// </summary>
        public readonly Dimension Dimension;

        public GLTextureHandle(int index, GLTexture texture, Box2F uv, Dimension dimension)
        {
            Index = index;
            Texture = texture;
            UV = uv;
            Dimension = dimension;
        }
    }
}
