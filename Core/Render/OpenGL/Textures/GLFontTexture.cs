using Helion.Graphics.Fonts;

namespace Helion.Render.OpenGL.Textures
{
    /// <summary>
    /// A texture for a font.
    /// </summary>
    public class GLFontTexture
    {
        public readonly GLTexture Texture;
        private readonly IFont m_font;
        
        public GLFontTexture(GLTexture texture, IFont font)
        {
            Texture = texture;
            m_font = font;
        }
    }
}
