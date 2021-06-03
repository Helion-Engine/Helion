using Helion.Graphics.Fonts;

namespace Helion.Render.OpenGL.Textures
{
    public record GLFontTexture<GLTextureT>(IFont Font, GLTextureT Texture) 
        where GLTextureT : GLTexture
    {
    }
}
