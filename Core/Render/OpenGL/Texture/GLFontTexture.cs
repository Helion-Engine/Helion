using System;
using Helion;
using Helion.Graphics.Fonts;
using Helion.Render;
using Helion.Render.OpenGL.Texture;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Texture;

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

    public void Dispose()
    {
        Texture.Dispose();
        GC.SuppressFinalize(this);
    }
}
