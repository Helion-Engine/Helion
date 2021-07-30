using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics.New;
using Helion.Render.OpenGL.Capabilities;
using Helion.Util.Extensions;
using NLog;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Textures.Types
{
    public class GLTexture2D : GLTexture
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly bool UsesMipmapTexParameter = !GLCapabilities.Version.Supports(3, 0);

        public readonly Dimension Dimension;
        public readonly string DebugName;

        // TODO: Refactor both these constructors!
        public GLTexture2D(Dimension dimension, string debugName = "") : base(TextureTarget.Texture2D)
        {
            Dimension = dimension;
            DebugName = debugName;

            BindAnd(() =>
            {
                if (UsesMipmapTexParameter)
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 1);

                SetDefaultTextureParameters();

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, dimension.Width,
                    dimension.Height, 0, PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, IntPtr.Zero);

                GenerateMipmaps(false);
            });
        }

        public GLTexture2D(Image image, string debugName = "") : base(TextureTarget.Texture2D)
        {
            Dimension = image.Dimension;
            DebugName = debugName;

            BindAnd(() =>
            {
                if (UsesMipmapTexParameter)
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 1);

                SetDefaultTextureParameters();

                image.Bitmap.WithLockedBits(data =>
                {
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, image.Dimension.Width,
                        image.Dimension.Height, 0, PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, data);
                });

                GenerateMipmaps(false);
            });
        }

        private void SetDefaultTextureParameters()
        {
            SetWrapMode(TextureWrapMode.Clamp, false);
            SetFilterMode(TextureMinFilter.Nearest, TextureMagFilter.Nearest, false);
        }

        public void Upload(Vec2I origin, Image image, bool generateMipmap, bool bind)
        {
            if (bind)
                Bind();

            if (origin.X < 0 || origin.Y < 0 || origin.X >= Dimension.Width || origin.Y >= Dimension.Height)
            {
                Log.Error($"Uploading image to GL texture '{DebugName}' origin out of range (origin: {origin}, dimension: {Dimension})");
                return;
            }

            (int w, int h) = image.Dimension;
            if (origin.X + w > Dimension.Width || origin.Y + h > Dimension.Height)
            {
                Log.Error($"Uploading image to GL texture '{DebugName}' out of range (origin: {origin}, dimension: {Dimension})");
                w = Dimension.Width - origin.X;
                h = Dimension.Height - origin.Y;
            }

            image.Bitmap.WithLockedBits(data =>
            {
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, origin.X, origin.Y, w, h, PixelFormat.Bgra,
                    PixelType.UnsignedInt8888Reversed, data);

                if (generateMipmap)
                    GenerateMipmaps(false);
            });

            if (bind)
                Unbind();
        }

        public void SetWrapMode(TextureWrapMode wrapMode, bool bind)
        {
            if (bind)
                Bind();

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int)wrapMode);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapMode);

            if (bind)
                Unbind();
        }

        public void SetFilterMode(TextureMinFilter minFilter, TextureMagFilter magFilter, bool bind)
        {
            if (bind)
                Bind();

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);

            if (bind)
                Unbind();
        }

        public void SetAnisotropicFilteringMode(float value, bool bind)
        {
            if (!GLCapabilities.Limits.Anisotropy.Supported)
                return;

            if (bind)
                Bind();

            GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, value);

            if (bind)
                Unbind();
        }

        public void GenerateMipmaps(bool bind)
        {
            if (!UsesMipmapTexParameter)
                return;

            if (bind)
                Bind();

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            if (bind)
                Unbind();
        }
    }
}
