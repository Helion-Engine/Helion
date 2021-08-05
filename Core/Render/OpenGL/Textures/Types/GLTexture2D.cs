using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Capabilities;
using Helion.Render.OpenGL.Util;
using Helion.Util.Extensions;
using NLog;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using Image = Helion.Graphics.Image;

namespace Helion.Render.OpenGL.Textures.Types
{
    public class GLTexture2D : GLTexture
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly bool UsesMipmapTexParameter = !GLCapabilities.Extensions.GenerateMipmapsFunction;

        public readonly Dimension Dimension;

        public GLTexture2D(string debugName, Dimension dimension) : base(debugName, TextureTarget.Texture2D)
        {
            Log.Debug("Creating {Type} ({Dim}, {Name})", nameof(GLTexture2D), dimension, debugName);

            Dimension = dimension;

            SetInitialData(dimension);
        }

        public GLTexture2D(string debugName, Image image) : base(debugName, TextureTarget.Texture2D)
        {
            Log.Debug("Creating {Type} from image ({Dim}, {Name})", nameof(GLTexture2D), image.Dimension, debugName);

            Dimension = image.Dimension;

            image.Bitmap.WithLockedBits(data =>
            {
                SetInitialData(image.Dimension, data);
            });
        }

        private void SetInitialData(Dimension dimension, IntPtr? data = null)
        {
            BindAnd(() =>
            {
                if (UsesMipmapTexParameter)
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 1);

                SetWrapMode(TextureWrapMode.Clamp, Binding.DoNotBind);
                SetFilterMode(TextureMinFilter.Nearest, TextureMagFilter.Nearest, Binding.DoNotBind);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, dimension.Width,
                    dimension.Height, 0, PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, 
                    data ?? IntPtr.Zero);

                GenerateMipmaps(Binding.DoNotBind);
            });
        }

        public void Upload(Vec2I origin, Image image, Mipmap mipmap, Binding bind)
        {
            if (origin.X < 0 || origin.Y < 0 || origin.X >= Dimension.Width || origin.Y >= Dimension.Height)
            {
                Log.Error("Uploading image to {Type} {Name} origin out of range ({Origin}, {Dimension})", nameof(GLTexture2D), DebugName, origin, image.Dimension);
                return;
            }

            (int w, int h) = image.Dimension;
            if (origin.X + w > Dimension.Width || origin.Y + h > Dimension.Height)
            {
                Log.Error("Uploading image to {Type} {Name} out of range ({Origin}, {Dimension})", nameof(GLTexture2D), DebugName, origin, image.Dimension);
                w = Dimension.Width - origin.X;
                h = Dimension.Height - origin.Y;
            }

            BindConditional(bind, () =>
            {
                Log.Trace("Uploading data to {Type} {DebugName}, at {Origin} with {Dimension} pixels", nameof(GLTexture2D), DebugName, origin, image.Dimension);

                image.Bitmap.WithLockedBits(data =>
                {
                    GL.TexSubImage2D(TextureTarget.Texture2D, 0, origin.X, origin.Y, w, h, PixelFormat.Bgra,
                        PixelType.UnsignedInt8888Reversed, data);

                    if (mipmap == Mipmap.Generate)
                        GenerateMipmaps(Binding.DoNotBind);
                });
            });
        }

        public void SetWrapMode(TextureWrapMode wrapMode, Binding bind)
        {
            BindConditional(bind, () =>
            {
                Log.Trace("Setting {Wrap} for {Type} {DebugName}", wrapMode, nameof(GLTexture2D), DebugName);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int)wrapMode);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapMode);
            });
        }

        public void SetFilterMode(TextureMinFilter minFilter, TextureMagFilter magFilter, Binding bind)
        {
            BindConditional(bind, () =>
            {
                Log.Trace("Setting filters {Min} {Mag} for {Type} {DebugName}", minFilter, magFilter, nameof(GLTexture2D), DebugName);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
            });
        }

        public void SetAnisotropicFilteringMode(float value, Binding bind)
        {
            if (!GLCapabilities.Limits.Anisotropy.Supported)
                return;

            if (value < 1.0 || value > GLCapabilities.Limits.Anisotropy.Max)
            {
                Log.Warn("Anisotropic value {Value} is out of range when setting {Type} {DebugName}", value, nameof(GLTexture2D), DebugName);
                value = Math.Clamp(value, 1.0f, GLCapabilities.Limits.Anisotropy.Max);
            }

            BindConditional(bind, () =>
            {
                Log.Trace("Setting anisotropic filtering to {Value} for {Type} {DebugName}", value, nameof(GLTexture2D), DebugName);
                GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, value);
            });
        }

        public void GenerateMipmaps(Binding bind)
        {
            if (!UsesMipmapTexParameter)
                return;

            BindConditional(bind, () =>
            {
                Log.Trace("Generating mipmaps for {Type} {DebugName}", nameof(GLTexture2D), DebugName);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            });
        }

        public Bitmap DownloadPixels()
        {
            Bitmap bitmap = new(Dimension.Width, Dimension.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            BindAnd(() =>
            {
                bitmap.WithLockedBits(data =>
                {
                    GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Bgra, PixelType.Byte, data);
                }); 
            });
            
            return bitmap;
        }
    }
}
