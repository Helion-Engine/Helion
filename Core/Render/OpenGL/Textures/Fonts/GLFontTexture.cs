using System;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures.Fonts
{
    public class GLFontTexture<GLTextureType> : IDisposable where GLTextureType : GLTexture
    {
        public readonly GLTextureType Texture;
        public readonly GLFontMetrics Metrics;

        public GLFontTexture(GLTextureType texture, GLFontMetrics metrics)
        {
            Texture = texture;
            Metrics = metrics;
        }

        ~GLFontTexture()
        {
            FailedToDispose(this);
            Dispose();
        }

        public GLGlyph this[char c] => Metrics[c];

        public void Dispose()
        {
            Texture.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}