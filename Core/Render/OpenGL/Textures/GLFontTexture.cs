using Helion.Graphics.Fonts;
using Helion.Render.OpenGL.Textures.Types;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Textures
{
    /// <summary>
    /// A texture for a font.
    /// </summary>
    public class GLFontTexture : GLTexture2D
    {
        public readonly Font Font;

        public GLFontTexture(string debugName, Font font) : base(debugName, font.Image)
        {
            Font = font;
            
            if (font.IsTrueTypeFont)
            {
                BindAnd(() =>
                {
                    SetFilterMode(TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear, Binding.DoNotBind);
                    SetAnisotropicFilteringMode(16.0f, Binding.DoNotBind);    
                });
            }
        }
    }
}
