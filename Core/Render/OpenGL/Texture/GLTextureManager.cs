using Helion.Graphics;
using Helion.Projects;
using Helion.Render.OpenGL.Util;
using Helion.Resources;
using Helion.Resources.Images;
using Helion.Util;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Texture
{
    public class GLTextureManager : IDisposable
    {
        public readonly GLTexture NullTexture;
        private bool disposed = false;
        private readonly float maxAnisotropy;
        private readonly Project project;
        private readonly ResourceTracker<GLTexture> textures = new ResourceTracker<GLTexture>();
        private readonly DynamicArray<GLTextureHandle> handles = new DynamicArray<GLTextureHandle>();

        public GLTextureManager(GLInfo glInfo, Project targetProject)
        {
            if (!glInfo.Version.Supports(4, 4))
                throw new HelionException($"OpenGL version too outdated (you have {glInfo.Version}, need 4.4+)");

            project = targetProject;
            maxAnisotropy = GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy);
            NullTexture = CreateTexture("NULL", ResourceNamespace.Global, ImageHelper.CreateNullImage(), false);

            project.Resources.ImageManager.ImageEventEmitter += HandleImageEvent;
        }

        ~GLTextureManager()
        {
            Dispose(false);
        }

        private int CalculateMaxMipmapLevels(Image image)
        {
            Assert.Precondition(image.Width > 0 && image.Height > 0, "Cannot make mipmap from a zero dimension image");

            int minDimension = Math.Min(image.Width, image.Height);
            int levels = (int)Math.Log(minDimension, 2.0);
            return Math.Max(1, levels);
        }

        private GLTexture CreateTexture(UpperString name, ResourceNamespace resourceNamespace, Image image, bool trackable = true)
        {
            Assert.Precondition(!textures.Contains(name, resourceNamespace), $"Accidentally creating a texture and overwriting it: {name} in {resourceNamespace}");

            int textureName = GL.GenTexture();
            int maxMipmapLevels = CalculateMaxMipmapLevels(image);

            GL.BindTexture(TextureTarget.Texture2D, textureName);

            GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, textureName, $"Texture: {name}");
            AllocateTextureStorage(image, maxMipmapLevels);
            UploadTexturePixels(image);
            SetTextureParameters(resourceNamespace);
            SetTextureAnisostropy();
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            long residentHandle = CreateBindlessHandle(textureName);

            GL.BindTexture(TextureTarget.Texture2D, 0);

            // TODO: Right now we're always placing it at the end of the list. 
            // In the future we can reuse deleted indices if any are available.
            int handleIndex = handles.Length;
            GLTextureHandle textureHandle = new GLTextureHandle(residentHandle, image.Width, image.Height);
            GLTexture texture = new GLTexture(textureName, handleIndex, textureHandle);

            // We made this a flag because we want to be able to add textures
            // that the user can't override (like the null texture). Note that
            // anything which is false and not added here needs to manually be
            // cleaned up.
            if (trackable)
                textures.AddOrOverwrite(name, resourceNamespace, texture);

            handles.Add(textureHandle);

            return texture;
        }

        private void AllocateTextureStorage(Image image, int maxMipmapLevels)
        {
            GL.TexStorage2D(TextureTarget2d.Texture2D, maxMipmapLevels, SizedInternalFormat.Rgba8, 
                image.Width, image.Height);
        }

        private void UploadTexturePixels(Image image)
        {
            var pixelArea = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
            var lockMode = System.Drawing.Imaging.ImageLockMode.ReadOnly;
            var format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            System.Drawing.Imaging.BitmapData bitmapData = image.Bitmap.LockBits(pixelArea, lockMode, format);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, image.Width, image.Height, PixelFormat.Rgba, PixelType.UnsignedInt8888, bitmapData.Scan0);

            image.Bitmap.UnlockBits(bitmapData);
        }

        private void SetTextureAnisostropy()
        {
            TextureParameterName anisostropyPname = (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt;
            
            // TODO: Use the value in the config here with min(configValue, maxAnisotropy).
            GL.TexParameter(TextureTarget.Texture2D, anisostropyPname, maxAnisotropy);
        }

        private void SetTextureParameters(ResourceNamespace resourceNamespace)
        {
            // For now, sprites need to be their own special blocky kind since blending
            // with alpha screws things up. Until order independent transparency is a
            // thing, we can't handle sprites without blocks because of the bleeding
            // through.
            if (resourceNamespace == ResourceNamespace.Sprites)
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            }
            else
            {
                // TODO: This should be based off of the config value.
                TextureMinFilter minParam = TextureMinFilter.LinearMipmapLinear;
                TextureMagFilter magParam = GLHelper.MinToMagFilter(minParam);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minParam);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magParam);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            }
        }

        private long CreateBindlessHandle(int textureName)
        {
            long residentHandle = GL.Arb.GetTextureHandle(textureName);
            GL.Arb.MakeTextureHandleResident(residentHandle);
            return residentHandle;
        }

        private void UpdateTextureData(UpperString name, ResourceNamespace resourceNamespace, Image image)
        {
            // TODO
        }

        private void CreateOrUpdateTexture(UpperString name, ResourceNamespace resourceNamespace, Image image)
        {
            if (textures.Contains(name, resourceNamespace))
                UpdateTextureData(name, resourceNamespace, image);
            else
                CreateTexture(name, resourceNamespace, image);
        }

        private void DeleteTexture(UpperString name, ResourceNamespace resourceNamespace)
        {
            // TODO
        }

        private void DestroyTexture(GLTexture texture)
        {
            GL.Arb.MakeTextureHandleNonResident(texture.Handle.ResidentHandle);
            GL.DeleteTexture(texture.Name);
        }

        private void DestroyAllTextures()
        {
            DestroyTexture(NullTexture);
            foreach (var textureEntry in textures)
                DestroyTexture(textureEntry.Value);
        }

        private void HandleImageEvent(object sender, ImageManagerEventArgs imageEvent)
        {
            switch (imageEvent.Type)
            {
            case ImageManagerEventType.CreateOrUpdate:
                CreateOrUpdateTexture(imageEvent.Name, imageEvent.Namespace, imageEvent.Image.Value);
                break;
            case ImageManagerEventType.Delete:
                DeleteTexture(imageEvent.Name, imageEvent.Namespace);
                break;
            default:
                throw new NotSupportedException("Unexpected image event enumeration");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                DestroyAllTextures();
                project.Resources.ImageManager.ImageEventEmitter -= HandleImageEvent;
            }

            disposed = true;
        }
    }
}
