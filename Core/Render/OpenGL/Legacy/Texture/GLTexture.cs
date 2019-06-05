using Helion.Util.Geometry;
using OpenTK.Graphics.OpenGL;
using System;
using System.Numerics;

namespace Helion.Render.OpenGL.Legacy.Texture
{
    public class GLTexture
    {
        public readonly Dimension Dimension;
        public readonly Vector2 InverseUV;
        public readonly int Handle;

        public GLTexture(int handle, Dimension dimension)
        {
            Dimension = dimension;
            InverseUV = new Vector2(1.0f / dimension.Width, 1.0f / dimension.Height);
            Handle = handle;
        }

        public void Bind() => GL.BindTexture(TextureTarget.Texture2D, Handle);

        public static void Unbind() => GL.BindTexture(TextureTarget.Texture2D, 0);

        public void BindAnd(Action action)
        {
            Bind();
            action.Invoke();
            Unbind();
        }
    }
}
