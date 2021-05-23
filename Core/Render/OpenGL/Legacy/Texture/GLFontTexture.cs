using System;
using Helion.Graphics.Fonts;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Legacy.Texture
{
    public class GLFontTexture<GLTextureType> : IDisposable where GLTextureType : GLTexture
    {
        public readonly GLTextureType Texture;
        public readonly Font Font;

        public int Height => Font.MaxHeight;

        public GLFontTexture(GLTextureType texture, Font font)
        {
            Texture = texture;
            Font = font;
        }

        ~GLFontTexture()
        {
            FailedToDispose(this);
            Dispose();
        }

        public FontGlyph this[char c] => Font[c];

        public void Dispose()
        {
            Texture.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}