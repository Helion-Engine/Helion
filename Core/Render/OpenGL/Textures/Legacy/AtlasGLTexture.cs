using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Graphics.New;
using Helion.Render.OpenGL.Capabilities;
using Helion.Util.Atlas;
using Helion.Util.Extensions;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Textures.Legacy
{
    public class AtlasGLTexture : GLTexture
    {
        public Dimension Dimension { get; private set; }
        private readonly Atlas2D m_atlas;

        public AtlasGLTexture() : base(TextureTarget.Texture2D)
        {
            int dim = Math.Min(GLCapabilities.Limits.MaxTexture2DSize, 4096);
            Dimension = (dim, dim);
            m_atlas = new Atlas2D(Dimension);

            BindAnd(() =>
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, Dimension.Width, 
                    Dimension.Height, 0, PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, IntPtr.Zero);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.Clamp);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
                GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            });
        }

        internal bool TryUpload(Image image, out Box2I area)
        {
            area = default;

            AtlasHandle? atlasHandle = m_atlas.Add(image.Dimension);
            if (atlasHandle == null)
                return false;

            BindAnd(() =>
            {
                image.Bitmap.WithLockedBits(data =>
                {
                    (int x, int y) = atlasHandle.Location.Min;
                    (int width, int height) = atlasHandle.Location.Dimension;

                    GL.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, width, height, PixelFormat.Bgra,
                        PixelType.UnsignedInt8888Reversed, data);
                });
            });

            area = atlasHandle.Location;
            return true;
        }
    }
}
