using Helion.Graphics;
using Helion.Projects;
using Helion.Render.OpenGL.Shared;
using Helion.Util;
using Helion.Util.Geometry;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Legacy.Texture
{
    public class GLLegacyTextureManager : GLTextureManager
    {
        public readonly GLTexture NullTexture;
        protected bool disposed;
        private readonly List<GLTexture> textures = new List<GLTexture>();
        private readonly Dictionary<UpperString, GLTexture> nameToTexture = new Dictionary<UpperString, GLTexture>();

        public GLLegacyTextureManager(Project project)
        {
            NullTexture = CreateTexture(ImageHelper.CreateNullImage());

            // TODO: Check for anisostropy/extension
            // TODO: Register with project for getting updates
            // TODO: Unregister on destruction
        }

        ~GLLegacyTextureManager() => Dispose(false);

        private void SetTextureParameters()
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        }

        private GLTexture CreateTexture(Image image)
        {
            GLTexture texture = new GLTexture(GL.GenTexture(), new Dimension(image.Width, image.Height));

            texture.BindAnd(() =>
            {
                UploadTexturePixels(image);
                SetTextureParameters();
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            });

            return texture;
        }

        private void DestroyTexture(GLTexture texture) => GL.DeleteTexture(texture.Handle);

        private void DestroyAllTextures()
        {
            textures.ForEach(DestroyTexture);
            textures.Clear();
            nameToTexture.Clear();
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

        protected override void PerformTextureUpload(Image image, IntPtr dataPtr)
        {
            // Because the C# image format is 'ARGB', we can get it into the 
            // RGBA format by doing a BGRA format and then reversing it.
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 
                          0, PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, dataPtr);
        }

        protected virtual void Dispose(bool disposing)
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

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
