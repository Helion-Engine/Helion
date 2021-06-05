using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Graphics.Fonts;

namespace Helion.Render.OpenGL.Textures
{
    /// <summary>
    /// A texture handle for a font.
    /// </summary>
    public class GLFontTextureHandle : GLTextureHandle
    {
        private readonly IFont m_font;
        
        public GLFontTextureHandle(int index, GLTexture texture, Box2F uv, Dimension dimension, IFont font) : 
            base(index, texture, uv, dimension)
        {
            m_font = font;
        }
    }
}
