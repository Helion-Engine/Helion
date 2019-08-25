using System;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Texture.Fonts
{
    public abstract class GLFontTexture<GLTextureType> : IDisposable where GLTextureType : GLTexture
    {
        public abstract GLTextureType Texture { get; }
        public abstract GLFontMetrics Metrics { get; }

        ~GLFontTexture()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
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