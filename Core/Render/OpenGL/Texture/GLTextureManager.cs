using System;
using System.Collections.Generic;
using Helion.Graphics;
using Helion.Render.OpenGL.Buffer;
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
    /// <summary>
    /// An implementation of a texture manager in OpenGL.
    /// </summary>
    /// <typeparam name="GLTextureType">The type of GL texture.</typeparam>
    /// <typeparam name="DataType">The struct data type that will be placed in
    /// some kind of buffer for referencing.</typeparam>
    public abstract class GLTextureManager<GLTextureType, DataType> : IGLTextureManager 
        where GLTextureType : GLTexture 
        where DataType : struct
    {
        public readonly ShaderDataBufferObject<DataType> TextureData;
        protected readonly Config m_config;
        protected readonly ArchiveCollection m_archiveCollection;
        protected readonly IGLFunctions gl;
        protected readonly GLCapabilities Capabilities;
        private readonly IImageRetriever m_imageRetriever;
        private readonly List<GLTextureType?> m_textures = new List<GLTextureType?>();
        private readonly ResourceTracker<GLTextureType> m_textureTracker = new ResourceTracker<GLTextureType>();
        private readonly AvailableIndexTracker m_freeTextureIndex = new AvailableIndexTracker();

        public GLTexture NullTexture { get; }

        protected GLTextureManager(Config config, GLCapabilities capabilities, IGLFunctions functions, 
            ArchiveCollection archiveCollection)
        {
            m_config = config;
            m_archiveCollection = archiveCollection;
            m_imageRetriever = new ArchiveImageRetriever(m_archiveCollection);
            Capabilities = capabilities;
            gl = functions;
            NullTexture = CreateNullTexture();
            TextureData = CreateTextureDataBuffer();
        }
        
        ~GLTextureManager()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            Dispose();
        }
        
        public GLTexture Get(CIString name, ResourceNamespace priorityNamespace)
        {
            if (name == Constants.NoTexture)
                return NullTexture;
            
            GLTexture? textureForNamespace = m_textureTracker.GetOnly(name, priorityNamespace);
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
            GLTexture? anyTexture = m_textureTracker.Get(name, priorityNamespace);
            if (anyTexture != null) 
                return anyTexture;
            
            // Note that because we are getting any texture, we don't want to
            // use the provided namespace since if we ask for a flat, but get a
            // texture, and then index it as a flat... things probably go bad.
            Image? image = m_imageRetriever.Get(name, priorityNamespace);
            return image != null ? CreateTexture(image, name, image.Metadata.Namespace) : NullTexture;
        }

        public GLTexture GetWall(CIString name) => Get(name, ResourceNamespace.Textures);

        public GLTexture GetFlat(CIString name) => Get(name, ResourceNamespace.Flats);

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

        protected void SetAnisotropicFiltering()
        {
            if (!Capabilities.Extensions.TextureFilterAnisotropic || !m_config.Engine.Render.Anisotropy.Enable) 
                return;

            // TODO
//            float anisostropy = (float)m_config.Engine.Render.Anisotropy.Value;
//            if (m_config.Engine.Render.Anisotropy.UseMaxSupported)
//                anisostropy = m_capabilities.Limits.AnisotropyMax;
//
//            anisostropy = Math.Max(1.0f, Math.Min(anisostropy, Capabilities.Limits.AnisotropyMax));
//
//            const TextureParameterName anisotropyPname = (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt;
//            GL.TexParameter(TextureTarget.Texture2D, anisotropyPname, anisostropy);
//            throw new NotImplementedException("Need to add anisotropic filtering code");
        }

        protected void DeleteOldTextureIfAny(CIString name, ResourceNamespace resourceNamespace)
        {
            GLTexture? texture = m_textureTracker.GetOnly(name, resourceNamespace);
            if (texture == null)
                return;
            
            texture.Dispose();
            m_freeTextureIndex.MakeAvailable(texture.Id);
            m_textures[texture.Id] = null;
            m_textureTracker.Remove(name, resourceNamespace);
        }

        protected virtual void ReleaseUnmanagedResources()
        {
            TextureData.Dispose();
            NullTexture.Dispose();
            m_textures.ForEach(texture => texture?.Dispose());
        }

        protected abstract GLTextureType GenerateTexture(int id, Image image, CIString name, ResourceNamespace resourceNamespace);
        protected abstract ShaderDataBufferObject<DataType> CreateTextureDataBuffer();
        protected abstract void AddToTextureDataBuffer(GLTextureType texture);
        
        private GLTexture CreateNullTexture()
        {
            return GenerateTexture(0, ImageHelper.CreateNullImage(), "NULL", ResourceNamespace.Global);
        }
        
        private GLTexture CreateTexture(Image image, CIString name, ResourceNamespace resourceNamespace)
        {
            DeleteOldTextureIfAny(name, resourceNamespace);

            int id = m_freeTextureIndex.Next();
            GLTextureType texture = GenerateTexture(id, image, name, resourceNamespace);
            m_textureTracker.Insert(name, resourceNamespace, texture);
            AddToTextureList(id, texture);
            
            AddToTextureDataBuffer(texture);

            return texture;
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
    }
}