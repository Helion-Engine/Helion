using System;
using System.Drawing.Imaging;
using Helion.Graphics;
using Helion.Render.OpenGL.Util;
using Helion.Resources;
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
        
        /// <summary>
        /// The OpenGL texture 'name'.
        /// </summary>
        private readonly int m_atlasTextureHandle;
        
        /// <summary>
        /// The config with settings to apply to textures.
        /// </summary>
        private readonly Config m_config;
        
        /// <summary>
        /// A collection of OpenGL capabilities.
        /// </summary>
        private readonly GLCapabilities m_capabilities;
        
        /// <summary>
        /// The atlas that manages the space for where textures should go with
        /// respect to some 2D area.
        /// </summary>
        private readonly Atlas2D m_atlas;
        
        /// <summary>
        /// A list of all the tracked resources.
        /// </summary>
        private readonly ResourceTracker<GLTexture> m_textures = new ResourceTracker<GLTexture>();

        /// <summary>
        /// Creates a texture manager using the config and GL info provided.
        /// </summary>
        /// <param name="config">The config for texture parameters.</param>
        /// <param name="capabilities">The OpenGL capabilities.</param>
        public GLTextureManager(Config config, GLCapabilities capabilities)
        {
            m_config = config;
            m_capabilities = capabilities;
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
        public GLTexture Get(CiString name, ResourceNamespace priorityNamespace)
        {
            return m_textures.GetWithAny(name, priorityNamespace) ?? NullTextureHandle;
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
        public GLTexture GetWallTexture(CiString name) => Get(name, ResourceNamespace.Textures);
        
        /// <summary>
        /// Gets the texture, with priority given to the flat namespace. If it
        /// cannot be found, the null texture handle is returned.
        /// </summary>
        /// <param name="name">The flat texture name.</param>
        /// <returns>The handle for the texture in the provided namespace, or
        /// the texture in another namespace if the texture was not found in
        /// the desired namespace, or the null texture if no such texture was
        /// found with the name provided.</returns>
        public GLTexture GetFlatTexture(CiString name) => Get(name, ResourceNamespace.Flats);

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

        private GLTexture? CreateTexture(Image image, CiString name, ResourceNamespace resourceNamespace)
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
            
            m_textures.AddOrOverwrite(name, resourceNamespace, texture);

            return texture;
        }

        private void DeleteTexture(CiString name, ResourceNamespace resourceNamespace)
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