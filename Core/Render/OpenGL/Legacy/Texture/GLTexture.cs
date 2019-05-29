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

        internal void Bind(TextureUnit textureUnit)
        {
            GL.ActiveTexture(textureUnit);
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }

        internal void Unbind() => GL.BindTexture(TextureTarget.Texture2D, 0);

        public void BindAnd(Action action, TextureUnit textureUnit = TextureUnit.Texture0)
        {
            Bind(textureUnit);
            action.Invoke();
            Unbind();
        }
    }
}
