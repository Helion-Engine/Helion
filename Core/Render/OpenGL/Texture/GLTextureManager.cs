using System;
using System.Collections.Generic;
using Helion.Graphics;
using Helion.Render.OpenGL.Context;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Images;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Container;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Texture
{
    public abstract class GLTextureManager<GLTextureType> : IGLTextureManager where GLTextureType : GLTexture
    {
        protected readonly Config Config;
        protected readonly ArchiveCollection ArchiveCollection;
        protected readonly GLCapabilities Capabilities;
        protected readonly IGLFunctions gl;
        private readonly IImageRetriever m_imageRetriever;
        private readonly List<GLTextureType?> m_textures = new List<GLTextureType?>();
        private readonly ResourceTracker<GLTextureType> m_textureTracker = new ResourceTracker<GLTextureType>();
        private readonly AvailableIndexTracker m_freeTextureIndex = new AvailableIndexTracker();
        
        /// <summary>
        /// The null texture, intended to be used when the actual texture
        /// cannot be found.
        /// </summary>
        public GLTextureType NullTexture { get; }
        
        protected GLTextureManager(Config config, GLCapabilities capabilities, IGLFunctions functions, 
            ArchiveCollection archiveCollection)
        {
            Config = config;
            ArchiveCollection = archiveCollection;
            m_imageRetriever = new ArchiveImageRetriever(ArchiveCollection);
            Capabilities = capabilities;
            gl = functions;
            NullTexture = CreateNullTexture();
        }
        
        ~GLTextureManager()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            Dispose();
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
        public GLTextureType Get(CIString name, ResourceNamespace priorityNamespace)
        {
            if (name == Constants.NoTexture)
                return NullTexture;
            
            GLTextureType? textureForNamespace = m_textureTracker.GetOnly(name, priorityNamespace);
            if (textureForNamespace != null) 
                return textureForNamespace;

            // The reason we do this check before checking other namespaces is
            // that we can end up missing the texture for the namespace in some
            // pathological scenarios. Suppose we draw some texture that shares
            // a name with some flat. Then suppose we try to draw the flat. If
            // we check the GL texture cache first, we will find the texture
            // and miss the flat and then never know that there is a specific
            // flat that should have been used.
            Image? imageForNamespace = m_imageRetriever.GetOnly(name, priorityNamespace);
            if (imageForNamespace != null) 
                return CreateTexture(imageForNamespace, name, priorityNamespace);

            // Now that nothing in the desired namespace was found, we will
            // accept anything.
            GLTextureType? anyTexture = m_textureTracker.Get(name, priorityNamespace);
            if (anyTexture != null) 
                return anyTexture;
            
            // Note that because we are getting any texture, we don't want to
            // use the provided namespace since if we ask for a flat, but get a
            // texture, and then index it as a flat... things probably go bad.
            Image? image = m_imageRetriever.Get(name, priorityNamespace);
            return image != null ? CreateTexture(image, name, image.Metadata.Namespace) : NullTexture;
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
        public GLTextureType GetWall(CIString name) => Get(name, ResourceNamespace.Textures);

        /// <summary>
        /// Gets the texture, with priority given to the flat namespace. If it
        /// cannot be found, the null texture handle is returned.
        /// </summary>
        /// <param name="name">The flat texture name.</param>
        /// <returns>The handle for the texture in the provided namespace, or
        /// the texture in another namespace if the texture was not found in
        /// the desired namespace, or the null texture if no such texture was
        /// found with the name provided.</returns>
        public GLTextureType GetFlat(CIString name) => Get(name, ResourceNamespace.Flats);
                
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        protected static int CalculateMipmapLevels(Dimension dimension)
        {
            int smallerAxis = Math.Min(dimension.Width, dimension.Height);
            return (int)Math.Floor(Math.Log(smallerAxis, 2));
        }
        
        protected virtual GLTextureType CreateTexture(Image image, CIString name, ResourceNamespace resourceNamespace)
        { 
            DeleteOldTextureIfAny(name, resourceNamespace);

            int id = m_freeTextureIndex.Next();
            GLTextureType texture = GenerateTexture(id, image, name, resourceNamespace);
            m_textureTracker.Insert(name, resourceNamespace, texture);
            AddToTextureList(id, texture);

            return texture;
        }
        
        protected virtual void DeleteTexture(GLTextureType texture, CIString name, ResourceNamespace resourceNamespace)
        {
            m_textures[texture.Id] = null;
            m_freeTextureIndex.MakeAvailable(texture.Id);
            m_textureTracker.Remove(name, resourceNamespace);
            texture.Dispose();
        }

        protected virtual void ReleaseUnmanagedResources()
        {
            NullTexture.Dispose();
            m_textures.ForEach(texture => texture?.Dispose());
        }

        protected abstract GLTextureType GenerateTexture(int id, Image image, CIString name, ResourceNamespace resourceNamespace);
        
        private GLTextureType CreateNullTexture()
        {
            return GenerateTexture(0, ImageHelper.CreateNullImage(), "NULL", ResourceNamespace.Global);
        }

        private void AddToTextureList(int id, GLTextureType texture)
        {
            if (id == m_textures.Count)
            {
                m_textures.Add(texture);
                return;
            }

            Invariant(id == m_textures.Count, $"Trying to add texture to an invalid index: {id} (count = {m_textures.Count})");
            m_textures[id] = texture;
        }
        
        private void DeleteOldTextureIfAny(CIString name, ResourceNamespace resourceNamespace) 
        {
            GLTextureType? texture = m_textureTracker.GetOnly(name, resourceNamespace);
            if (texture != null)
                DeleteTexture(texture, name, resourceNamespace);
        }
    }
}