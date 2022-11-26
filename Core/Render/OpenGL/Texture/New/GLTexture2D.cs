using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Texture.New;

public class GLTexture2D : GLTexture
{
    public override void Bind()
    {
        GL.BindTexture(TextureTarget.Texture2D, Name);
    }

    public override void Unbind()
    {
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }
}
