using Helion.Util.Geometry;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Legacy.Texture
{
    public class GLTexture
    {
        public readonly Dimension Dimension;
        public readonly int Handle;

        public GLTexture(int handle, Dimension dimension)
        {
            Handle = handle;
            Dimension = dimension;
        }

        public void Bind(TextureUnit textureUnit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(textureUnit);
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }

        public static void Unbind() => GL.BindTexture(TextureTarget.Texture2D, 0);

        public void BindAnd(Action action, TextureUnit textureUnit = TextureUnit.Texture0)
        {
            Bind(textureUnit);
            action.Invoke();
            Unbind();
        }
    }
}
