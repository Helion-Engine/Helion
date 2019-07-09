using Helion.Configuration;
using Helion.Graphics;
using Helion.Projects;
using Helion.Projects.Resources;
using Helion.Render.Shared;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Geometry;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Texture
{
    public class GLTextureManager : IDisposable
    {
        public readonly GLTexture NullTexture;
        private bool disposed;
        private readonly GLInfo info;
        private readonly Config config;
        private readonly float AnisotropyMax = GL.GetFloat((GetPName)ExtTextureFilterAnisotropic.MaxTextureMaxAnisotropyExt);
        private readonly List<GLTexture> m_textures = new List<GLTexture>();
        private readonly Dictionary<CiString, GLTexture> m_nameToTexture = new Dictionary<CiString, GLTexture>();
        private readonly ProjectResources m_projectResources;

        public GLTextureManager(GLInfo glInfo, Config cfg, ProjectResources projectResources)
        {
            info = glInfo;
            config = cfg;
            m_projectResources = projectResources;
            NullTexture = CreateTexture(ImageHelper.CreateNullImage(), "NULL");
        }

        ~GLTextureManager() => Dispose(false);

        public void ClearCache()
        {
            DestroyAllTextures();
        }

        /// <summary>
        /// Calculates the proper max mipmap levels for the image.
        /// </summary>
        /// <param name="image">The image to get the mipmap levels for.</param>
        /// <returns>The best mipmap level value.</returns>
        private int CalculateMaxMipmapLevels(Image image)
        {
            Precondition(image.Width > 0 && image.Height > 0, "Cannot make mipmap from a zero dimension image");

            int minDimension = Math.Min(image.Width, image.Height);
            int levels = (int)Math.Log(minDimension, 2.0);
            return Math.Max(1, levels);
        }

        private void PerformTextureUpload(Image image, IntPtr dataPtr)
        {
            // Because the C# image format is 'ARGB', we can get it into the 
            // RGBA format by doing a BGRA format and then reversing it.
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height,
                          0, PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, dataPtr);
        }

        /// <summary>
        /// Uploads the image data to the currently bound 2D texture.
        /// </summary>
        /// <param name="image">The image to upload.</param>
        private void UploadTexturePixels(Image image)
        {
            var pixelArea = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
            var lockMode = System.Drawing.Imaging.ImageLockMode.ReadOnly;
            var format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            System.Drawing.Imaging.BitmapData bitmapData = image.Bitmap.LockBits(pixelArea, lockMode, format);

            PerformTextureUpload(image, bitmapData.Scan0);

            image.Bitmap.UnlockBits(bitmapData);
        }

        private void SetTextureParameters()
        {
            // TODO: This should be on some kind of callback from the config.
            TextureMinFilter minFilter = config.Engine.Render.Filter.Get().ToOpenTKTextureMinFilter();
            TextureMagFilter magFilter = GLHelper.MinToMagFilter(minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);

            // TODO: This should be 'clamp to edge' for sprites.
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            // TODO: This should be on some kind of callback from the config.
            if (info.Extensions.TextureFilterAnisotropic && config.Engine.Render.Anisotropy.Enable)
            {
                float anisostropy = (float)config.Engine.Render.Anisotropy.Value;
                if (config.Engine.Render.Anisotropy.UseMaxSupported)
                    anisostropy = AnisotropyMax;

                anisostropy = Math.Max(1.0f, Math.Min(anisostropy, AnisotropyMax));

                TextureParameterName anisotropyPname = (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt;
                GL.TexParameter(TextureTarget.Texture2D, anisotropyPname, anisostropy);
            }
        }

        private GLTexture CreateTexture(Image image, CiString name)
        {
            GLTexture texture = new GLTexture(GL.GenTexture(), new Dimension(image.Width, image.Height));

            texture.BindAnd(() =>
            {
                SetObjectLabel(texture.Handle, $"Texture {name}");
                UploadTexturePixels(image);
                SetTextureParameters();
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            });

            return texture;
        }

        private void SetObjectLabel(int textureHandle, string labelName)
        {
            if (info.Version.Supports(4, 3))
                GL.ObjectLabel(ObjectLabelIdentifier.Texture, textureHandle, labelName.Length, labelName);
        }

        private void TrackTexture(GLTexture texture, CiString name)
        {
            m_textures.Add(texture);
            m_nameToTexture[name] = texture;
        }

        private void DestroyTexture(GLTexture texture) => GL.DeleteTexture(texture.Handle);

        private void DestroyAllTextures()
        {
            m_textures.ForEach(DestroyTexture);
            m_nameToTexture.Clear();
            m_textures.Clear();

            m_nameToTexture["-"] = NullTexture;
        }

        public void DeleteTexture(CiString name)
        {
            if (m_nameToTexture.ContainsKey(name))
            {
                var texture = m_nameToTexture[name];
                DestroyTexture(texture);
                m_textures.Remove(texture);
                m_nameToTexture.Remove(name);
            }
        }

        public GLTexture Get(CiString name)
        {
            if (m_nameToTexture.ContainsKey(name))
            {
                return m_nameToTexture[name];
            }
            else
            {
                var image = m_projectResources.GetImage(name);
                if (image != null)
                {
                    GLTexture texture = CreateTexture(image, name);
                    TrackTexture(texture, name);
                    return texture;
                }
            }

            return NullTexture;
        }

        public GLTexture Get(int index)
        {
            if (index >= 0 && index < m_textures.Count)
                return m_textures[index];
            else
            {
                Fail($"Texture index out of range, {index} not in [0, {m_textures.Count})");
                return NullTexture;
            }
        }

        public void BindTextureIndex(TextureTarget target, int index) => GL.BindTexture(target, index);

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                DestroyAllTextures();
                DestroyTexture(NullTexture);
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
