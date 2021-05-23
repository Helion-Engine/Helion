using Helion.Graphics.Fonts;
using Helion.Render.OpenGL.Textures;
using OpenTK.Graphics.OpenGL4;

namespace Helion.Render.OpenGL.Modern.Textures
{
    public class ModernGLFontTexture : ModernGLTexture, IGLFontTexture
    {
        public IFont Font { get; }

        public ModernGLFontTexture(string name, IFont font) : 
            base(name, font.Image, TextureTarget.Texture2D)
        {
            Font = font;
        }
    }
}
