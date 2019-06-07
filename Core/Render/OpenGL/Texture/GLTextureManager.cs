using Helion.Graphics;
using Helion.Projects;
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
        private readonly float AnisotropyMax = GL.GetFloat((GetPName)ExtTextureFilterAnisotropic.MaxTextureMaxAnisotropyExt);
        private readonly List<GLTexture> textures = new List<GLTexture>();
        private readonly Dictionary<UpperString, GLTexture> nameToTexture = new Dictionary<UpperString, GLTexture>();

        public GLTextureManager(GLInfo glInfo, Project project)
        {
            info = glInfo;
            NullTexture = CreateTexture(ImageHelper.CreateNullImage(), "NULL", ResourceNamespace.Global);
        }

        ~GLTextureManager() => Dispose(false);

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
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLHelper.MinToMagFilter(TextureMinFilter.LinearMipmapLinear));

            // TODO: This should be 'clamp to edge' for sprites.
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            // TODO: We should check if this is supported first.
            TextureParameterName anisotropyPname = (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt;
            GL.TexParameter(TextureTarget.Texture2D, anisotropyPname, AnisotropyMax);
        }

        private GLTexture CreateTexture(Image image, UpperString name, ResourceNamespace resourceNamespace)
        {
            GLTexture texture = new GLTexture(GL.GenTexture(), new Dimension(image.Width, image.Height));

            texture.BindAnd(() =>
            {
                SetObjectLabel(texture.Handle, $"Texture [{resourceNamespace}]: {name}");
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

        private void TrackTexture(GLTexture texture, UpperString name, ResourceNamespace resourceNamespace)
        {
            textures.Add(texture);

            // TODO: Support namespace handling as well.
            nameToTexture[name] = texture;
        }

        private void DestroyTexture(GLTexture texture) => GL.DeleteTexture(texture.Handle);

        private void DestroyAllTextures()
        {
            textures.ForEach(DestroyTexture);
            textures.Clear();
            nameToTexture.Clear();
        }

        public void CreateOrUpdateTexture(Image image, UpperString name, ResourceNamespace resourceNamespace)
        {
            // TODO: Lookup by namespace as well, need a ResourceNamespaceTracker.
            if (nameToTexture.ContainsKey(name))
            {
                // TODO: Update if exists
            }
            else
            {
                GLTexture texture = CreateTexture(image, name, resourceNamespace);
                TrackTexture(texture, name, resourceNamespace);
            }
        }

        public void DeleteTexture(UpperString name)
        {
            // TODO
        }

        public GLTexture Get(UpperString name)
        {
            // TODO: Support namespaces as an optional parameter.
            return nameToTexture.TryGetValue(name, out GLTexture texture) ? texture : NullTexture;
        }

        public GLTexture Get(int index)
        {
            if (index >= 0 && index < textures.Count)
                return textures[index];
            else
            {
                Fail($"Texture index out of range, {index} not in [0, {textures.Count})");
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
