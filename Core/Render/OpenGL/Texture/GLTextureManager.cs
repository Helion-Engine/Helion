using System;
using System.Drawing.Imaging;
using Helion.Graphics;
using Helion.Render.OpenGL.Util;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Atlas;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Texture
{
    /// <summary>
    /// Manages all of the textures that are needed by OpenGL for rendering.
    /// </summary>
    /// <remarks>
    /// The current implementation uses a texture atlas, whereby the entire set
    /// of images are placed on one gigantic texture. This may have some size
    /// implications since the OS will likely need to buffer the texture. It
    /// will do its best to only use the space that is needed however.
    /// </remarks>
    public class GLTextureManager : IDisposable
    {
        /// <summary>
        /// A texture which represents a missing texture. It is the fallback
        /// when a texture cannot be found.
        /// </summary>
        public readonly GLTexture NullTextureHandle;

        /// <summary>
        /// A manager of texture buffer information for allocated GL textures.
        /// </summary>
        public readonly GLTextureDataBuffer TextureDataBuffer;
        
        private readonly int m_atlasTextureHandle;
        private readonly Config m_config;
        private readonly GLCapabilities m_capabilities;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly Atlas2D m_atlas;
        private readonly ResourceTracker<GLTexture> m_textures = new ResourceTracker<GLTexture>();

        /// <summary>
        /// Creates a texture manager using the config and GL info provided.
        /// </summary>
        /// <param name="config">The config for texture parameters.</param>
        /// <param name="capabilities">The OpenGL capabilities.</param>
        /// <param name="archiveCollection">The archive collection manager to
        /// get resources from.</param>
        public GLTextureManager(Config config, GLCapabilities capabilities, ArchiveCollection archiveCollection)
        {
            m_config = config;
            m_capabilities = capabilities;
            m_archiveCollection = archiveCollection;
            m_atlasTextureHandle = GL.GenTexture();
            m_atlas = new Atlas2D(GetBestAtlasDimension());
            TextureDataBuffer = new GLTextureDataBuffer(capabilities);

            AllocateTextureAtlasOnGPU();
            SetTextureAtlasParameters();
            
            NullTextureHandle = CreateNullTexture();
        }

        ~GLTextureManager()
        {
            ReleaseUnmanagedResources();
        }

        /// <summary>
        /// Gets the texture, with priority given to the namespace provided. If
        /// it cannot be found, the null texture handle is returned.
        /// </summary>
        /// <param name="name">The texture name.</param>
        /// <param name="priorityNamespace">The namespace to search first.
        /// </param>
        /// <returns>The handle for the texture in the provided namespace, or
        /// the texture in another namespace if the texture was not found in
        /// the desired namespace, or the null texture if no such texture was
        /// found with the name provided.</returns>
        public GLTexture Get(CIString name, ResourceNamespace priorityNamespace)
        {
            if (name == Constants.NoTexture)
                return NullTextureHandle;

            GLTexture? textureForNamespace = m_textures.GetOnly(name, priorityNamespace);
            if (textureForNamespace != null) 
                return textureForNamespace;
            
            // The reason we do this check before checking other namespaces is
            // that we can end up missing the texture for the namespace in some
            // pathological scenarios. Suppose we draw some texture that shares
            // a name with some flat. Then suppose we try to draw the flat. If
            // we check the GL texture cache first, we will find the texture
            // and miss the flat and then never know that there is a specific
            // flat that should have been used.
            Image? imageForNamespace = m_archiveCollection.Images.GetOnly(name, priorityNamespace);
            if (imageForNamespace != null) 
                return CreateTexture(imageForNamespace, name, priorityNamespace);

            // Now that nothing in the desired namespace was found, we will
            // accept anything.
            GLTexture? anyTexture = m_textures.Get(name, priorityNamespace);
            if (anyTexture != null) 
                return anyTexture;
            
            // Note that because we are getting any texture, we don't want to
            // use the provided namespace since if we ask for a flat, but get a
            // texture, and then index it as a flat... things probably go bad.
            Image? image = m_archiveCollection.Images.Get(name, priorityNamespace);
            return image != null ? CreateTexture(image, name, image.Metadata.Namespace) : NullTextureHandle;
        }

        /// <summary>
        /// Gets the texture, with priority given to the texture namespace. If
        /// it cannot be found, the null texture handle is returned.
        /// </summary>
        /// <param name="name">The texture name.</param>
        /// <returns>The handle for the texture in the provided namespace, or
        /// the texture in another namespace if the texture was not found in
        /// the desired namespace, or the null texture if no such texture was
        /// found with the name provided.</returns>
        public GLTexture GetWallTexture(CIString name) => Get(name, ResourceNamespace.Textures);
        
        /// <summary>
        /// Gets the texture, with priority given to the flat namespace. If it
        /// cannot be found, the null texture handle is returned.
        /// </summary>
        /// <param name="name">The flat texture name.</param>
        /// <returns>The handle for the texture in the provided namespace, or
        /// the texture in another namespace if the texture was not found in
        /// the desired namespace, or the null texture if no such texture was
        /// found with the name provided.</returns>
        public GLTexture GetFlatTexture(CIString name) => Get(name, ResourceNamespace.Flats);

        /// <summary>
        /// Binds both the texture unit and the texture for rendering.
        /// </summary>
        public void Bind()
        {
            GL.ActiveTexture(GLConstants.TextureAtlasUnit);
            BindTextureOnly();
        }

        /// <summary>
        /// Unbinds the texture.
        /// </summary>
        public void Unbind()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// Binds the texture to the provided texture unit, and carries out the
        /// function provided, and then unbinds.
        /// </summary>
        /// <param name="func">The function to call while bound.</param>
        public void BindAnd(Action func)
        {
            Bind();
            func.Invoke();
            Unbind();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
        
        private void AllocateTextureAtlasOnGPU()
        {
            BindTextureOnly();

            GLHelper.SetTextureLabel(m_capabilities, m_atlasTextureHandle, "Texture Atlas");
            
            // Because the C# image format is 'ARGB', we can get it into the
            // RGBA format by doing a BGRA format and then reversing it.
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          m_atlas.Dimension.Width, m_atlas.Dimension.Height,
                          0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                          PixelType.UnsignedInt8888Reversed, IntPtr.Zero);
            
            Unbind();
        }

        private void SetTextureAtlasParameters()
        {
            BindTextureOnly();
            
            // TODO: Needs to be updated with master, see the old GLTexManager
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            Unbind();
        }

        private GLTexture CreateNullTexture()
        {
            Image nullImage = ImageHelper.CreateNullImage();
            
            AtlasHandle? atlasHandle = m_atlas.Add(nullImage.Dimension);
            if (atlasHandle == null)
                throw new HelionException("Unable to allocate space in atlas for the null texture");

            UploadPixelsToAtlasTexture(nullImage, atlasHandle.Location);

            int textureDataHandle = TextureDataBuffer.AllocateTextureDataIndex();
            GLTexture texture = new GLTexture(textureDataHandle, m_atlas.Dimension, atlasHandle);
            TextureDataBuffer.Track(texture);
            
            return texture;
        }

        // We do not use this currently because it does not play nicely at
        // all with texture atlases.
        private void SetAnisotrophicFiltering()
        {
            if (!m_capabilities.Extensions.TextureFilterAnisotropic || !m_config.Engine.Render.Anisotropy.Enable) 
                return;
            
            float anisostropy = (float)m_config.Engine.Render.Anisotropy.Value;
            if (m_config.Engine.Render.Anisotropy.UseMaxSupported)
                anisostropy = m_capabilities.Limits.AnisotropyMax;

            anisostropy = Math.Max(1.0f, Math.Min(anisostropy, m_capabilities.Limits.AnisotropyMax));

            const TextureParameterName anisotropyPname = (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt;
            GL.TexParameter(TextureTarget.Texture2D, anisotropyPname, anisostropy);
        }
        
        private void BindTextureOnly()
        {
            GL.BindTexture(TextureTarget.Texture2D, m_atlasTextureHandle);
        }

        private Dimension GetBestAtlasDimension()
        {
            // We have to be a bit careful, because on GPUs with very large
            // texture sizes, we can end up allocating a ridiculous amount of
            // memory which likely has to be backed by the OS. We'd rather only
            // resize if we absolutely need to. We'll go with 4096 for now as
            // this is big enough to avoid lots of resizing.
            int atlasSize = Math.Min(m_capabilities.Limits.MaxTextureSize, 1024);
            return new Dimension(atlasSize, atlasSize);
        }

        private GLTexture CreateTexture(Image image, CIString name, ResourceNamespace resourceNamespace)
        {
            // We only want one image with this name/namespace in the texture
            // at a time. However we have some extra cleaning up to do if that
            // is the case, so we perform deletion.
            if (m_textures.Contains(name, resourceNamespace))
                DeleteTexture(name, resourceNamespace);

            AtlasHandle? atlasHandle = m_atlas.Add(image.Dimension);
            if (atlasHandle == null)
                throw new HelionException("Ran out of texture atlas space");

            UploadPixelsToAtlasTexture(image, atlasHandle.Location);
            
            int textureDataHandle = TextureDataBuffer.AllocateTextureDataIndex();
            GLTexture texture = new GLTexture(textureDataHandle, m_atlas.Dimension, atlasHandle);
            TextureDataBuffer.Track(texture);
            
            // TODO: We should never be overwriting...
            m_textures.Insert(name, resourceNamespace, texture);

            return texture;
        }

        private void DeleteTexture(CIString name, ResourceNamespace resourceNamespace)
        {
            GLTexture? handle = m_textures.GetOnly(name, resourceNamespace);
            if (handle == null)
                return;
            
            m_atlas.Remove(handle.AtlasHandle);
            m_textures.Remove(name, resourceNamespace);
            TextureDataBuffer.Remove(handle.TextureInfoIndex);
        }

        private void UploadPixelsToAtlasTexture(Image image, Box2I location)
        {
            // TODO: We should probably consider batch uploading so we don't
            //       keep binding/unbinding for every single texture upload.
            BindTextureOnly();
            
            var pixelArea = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
            var lockMode = ImageLockMode.ReadOnly;
            var format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            BitmapData bitmapData = image.Bitmap.LockBits(pixelArea, lockMode, format);

            // Because the C# image format is 'ARGB', we can get it into the 
            // RGBA format by doing a BGRA format and then reversing it.
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, location.BottomLeft.X, location.BottomLeft.Y, 
                             location.Dimension.Width, location.Dimension.Height, 
                             OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, 
                             bitmapData.Scan0);

            image.Bitmap.UnlockBits(bitmapData);
            
            Unbind();
        }

        private void ReleaseUnmanagedResources()
        {
            TextureDataBuffer.Dispose();
            GL.DeleteTexture(m_atlasTextureHandle);
        }
    }
}