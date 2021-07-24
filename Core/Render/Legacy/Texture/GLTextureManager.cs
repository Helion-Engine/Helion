using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Graphics;
using Helion.Graphics.Fonts;
using Helion.Render.Common.Textures;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Shared;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Images;
using Helion.Util;
using Helion.Util.Configs;
using MoreLinq;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Legacy.Texture
{
    public abstract class GLTextureManager<GLTextureType> : IGLTextureManager 
        where GLTextureType : GLTexture
    {
        protected readonly Config Config;
        protected readonly ArchiveCollection ArchiveCollection;
        protected readonly GLCapabilities Capabilities;
        protected readonly IGLFunctions gl;
        private readonly IImageRetriever m_imageRetriever;
        private readonly Dictionary<string, GLFontTexture<GLTextureType>> m_fonts = new(StringComparer.OrdinalIgnoreCase);
        private readonly ResourceTracker<GLTextureType> m_textureTracker = new();

        public abstract IImageDrawInfoProvider ImageDrawInfoProvider { get; }

        /// <summary>
        /// The null texture, intended to be used when the actual texture
        /// cannot be found.
        /// </summary>
        public GLTextureType NullTexture { get; }

        /// <summary>
        /// A fully white texture that can be used for drawing shapes of a
        /// solid color.
        /// </summary>
        public GLTextureType WhiteTexture { get; }

        /// <summary>
        /// The null sprite rotation for when a sprite cannot be found.
        /// </summary>
        public SpriteRotation NullSpriteRotation { get; }

        /// <summary>
        /// The null font, intended to be used when a font cannot be found.
        /// </summary>
        public GLFontTexture<GLTextureType> NullFont { get; }

        protected GLTextureManager(Config config, GLCapabilities capabilities, IGLFunctions functions,
            ArchiveCollection archiveCollection)
        {
            Config = config;
            ArchiveCollection = archiveCollection;
            m_imageRetriever = new ArchiveImageRetriever(ArchiveCollection);
            Capabilities = capabilities;
            gl = functions;
            NullTexture = CreateNullTexture();
            WhiteTexture = CreateWhiteTexture();
            NullSpriteRotation = CreateNullSpriteRotation();
            NullFont = CreateNullFont();
        }

        private SpriteRotation CreateNullSpriteRotation()
        {
            SpriteRotation spriteFrame = new(new Resources.Texture("NULL", ResourceNamespace.Sprites, 0), false);
            spriteFrame.Texture.RenderStore = NullTexture;
            return spriteFrame;
        }

        ~GLTextureManager()
        {
            FailedToDispose(this);
            Dispose();
        }

        public bool TryGet(string name, out IRenderableTextureHandle? handle, ResourceNamespace? specificNamespace = null)
        {
            if (TryGet(name, specificNamespace ?? ResourceNamespace.Global, out GLTextureType? texture))
            {
                handle = texture;
                return true;    
            }

            handle = null;
            return false;
        }

        /// <summary>
        /// Checks if the texture manager contains the image.
        /// </summary>
        /// <param name="name">The name of the image.</param>
        /// <returns>True if it does, false if not.</returns>
        public bool Contains(string name)
        {
            return TryGet(name, ResourceNamespace.Global, out _);
        }

        /// <summary>
        /// Gets the texture, with priority given to the namespace provided. If
        /// it cannot be found, the null texture handle is used instead.
        /// </summary>
        /// <param name="name">The texture name.</param>
        /// <param name="priorityNamespace">The namespace to search first.
        /// </param>
        /// <param name="texture">The populated texture. This will either be
        /// the texture you want, or it will be the null image texture.</param>
        /// <returns>True if the texture was found, false if it was not found
        /// and the out value is the null texture handle.</returns>
        public bool TryGet(string name, ResourceNamespace priorityNamespace, out GLTextureType texture)
        {
            texture = NullTexture;
            if (name == Constants.NoTexture)
                return false;

            GLTextureType? textureForNamespace = m_textureTracker.GetOnly(name, priorityNamespace);
            if (textureForNamespace != null)
            {
                texture = textureForNamespace;
                return true;
            }

            // The reason we do this check before checking other namespaces is
            // that we can end up missing the texture for the namespace in some
            // pathological scenarios. Suppose we draw some texture that shares
            // a name with some flat. Then suppose we try to draw the flat. If
            // we check the GL texture cache first, we will find the texture
            // and miss the flat and then never know that there is a specific
            // flat that should have been used.
            Image? imageForNamespace = m_imageRetriever.GetOnly(name, priorityNamespace);
            if (imageForNamespace != null)
            {
                texture = CreateTexture(imageForNamespace, name, priorityNamespace);
                return true;
            }

            // Now that nothing in the desired namespace was found, we will
            // accept anything.
            GLTextureType? anyTexture = m_textureTracker.Get(name, priorityNamespace);
            if (anyTexture != null)
            {
                texture = anyTexture;
                return true;
            }

            // Note that because we are getting any texture, we don't want to
            // use the provided namespace since if we ask for a flat, but get a
            // texture, and then index it as a flat... things probably go bad.
            Image? image = m_imageRetriever.Get(name, priorityNamespace);
            if (image == null)
                return false;

            texture = CreateTexture(image, name, image.Metadata.Namespace);
            return true;
        }

        public GLTextureType GetTexture(int index)
        {
            var texture = TextureManager.Instance.GetTexture(index);

            if (texture.RenderStore != null)
                return (GLTextureType)texture.RenderStore;

            if (texture.Image == null)
            {
                texture.RenderStore = CreateTexture(texture.Image);
                return (GLTextureType)texture.RenderStore;
            }

            texture.RenderStore = CreateTexture(texture.Image, texture.Name, texture.Image.Metadata.Namespace);
            return (GLTextureType)texture.RenderStore;
        }

        /// <summary>
        /// Get a sprite rotation.
        /// </summary>
        /// <param name="spriteDefinition">The sprite definition.</param>
        /// <param name="frame">Sprite frame.</param>
        /// <param name="rotation">Rotation.</param>
        /// <returns>Returns a SpriteRotation if sprite name, frame, and rotation are valid. Otherwise null.</returns>
        public SpriteRotation GetSpriteRotation(SpriteDefinition spriteDefinition, int frame, uint rotation)
        {
            SpriteRotation? spriteRotation = spriteDefinition.GetSpriteRotation(frame, rotation);
            if (spriteRotation == null)
                return NullSpriteRotation;

            if (spriteRotation.Texture.RenderStore == null)
                spriteRotation.Texture.RenderStore = CreateTexture(spriteRotation.Texture.Image);

            return spriteRotation;
        }

        /// <summary>
        /// Get a sprite definition.
        /// </summary>
        /// <param name="spriteName">Name of the sprite e.g. 'POSS' or 'SARG'.</param>
        public SpriteDefinition? GetSpriteDefinition(string spriteName)
        {
            SpriteDefinition? spriteDef = TextureManager.Instance.GetSpriteDefinition(spriteName);
            return spriteDef;
        }

        /// <summary>
        /// Gets the texture, with priority given to the sprite namespace. If
        /// it cannot be found, the null texture handle is returned.
        /// </summary>
        /// <param name="name">The flat texture name.</param>
        /// <param name="texture">The populated texture. This will either be
        /// the texture you want, or it will be the null image texture.</param>
        /// <returns>True if the texture was found, false if it was not found
        /// and the out value is the null texture handle.</returns>
        public bool TryGetSprite(string name, out GLTextureType texture)
        {
            return TryGet(name, ResourceNamespace.Sprites, out texture);
        }

        /// <summary>
        /// Gets the font for the name, or returns a default font that will
        /// contain null images.
        /// </summary>
        /// <param name="name">The name of the font.</param>
        /// <returns>The font for the provided name, otherwise the 'Null
        /// texture' (which isn't null but is made up of textures that are the
        /// missing texture image).</returns>
        public GLFontTexture<GLTextureType> GetFont(string name)
        {
            if (m_fonts.TryGetValue(name, out GLFontTexture<GLTextureType>? existingFontTexture))
                return existingFontTexture;

            Font? font = ArchiveCollection.GetFontDeprecated(name);
            if (font != null)
                return CreateNewFont(font, name);

            return NullFont;
        }

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

        protected GLTextureType CreateTexture(Image? image) => CreateTexture(image, null, ResourceNamespace.Global);

        protected GLTextureType CreateTexture(Image? image, string? name, ResourceNamespace resourceNamespace)
        {
            if (name != null)
                DeleteOldTextureIfAny(name, resourceNamespace);

            GLTextureType texture;
            if (image == null)
                texture = NullTexture;
            else
                texture = GenerateTexture(image, name ?? "", resourceNamespace);

            if (name != null)
                m_textureTracker.Insert(name, resourceNamespace, texture);

            return texture;
        }

        protected void DeleteTexture(GLTextureType texture, string name, ResourceNamespace resourceNamespace)
        {
            m_textureTracker.Remove(name, resourceNamespace);
            texture.Dispose();
        }

        protected void ReleaseUnmanagedResources()
        {
            NullTexture.Dispose();
            m_textureTracker.GetValues().ForEach(texture => texture?.Dispose());

            NullFont.Dispose();
            m_fonts.ForEach(pair => pair.Value.Dispose());
        }

        protected abstract GLTextureType GenerateTexture(Image image, string name, ResourceNamespace resourceNamespace);

        protected abstract GLFontTexture<GLTextureType> GenerateFont(Font font, string name);

        private GLTextureType CreateNullTexture()
        {
            return GenerateTexture(ImageHelper.CreateNullImage(), "NULL", ResourceNamespace.Global);
        }

        private GLTextureType CreateWhiteTexture()
        {
            return GenerateTexture(ImageHelper.CreateWhiteImage(), "NULL", ResourceNamespace.Global);
        }

        private GLFontTexture<GLTextureType> CreateNullFont()
        {
            const string NullFontName = "NULL";

            Image nullImage = ImageHelper.CreateNullImage();
            List<Glyph> glyphs = new() { new Glyph('?', nullImage) };
            FontMetrics metrics = new FontMetrics(nullImage.Height, nullImage.Height, 0, 0, 0);

            Font font = new Font(NullFontName, glyphs, metrics);
            return GenerateFont(font, NullFontName);
        }

        private void DeleteOldTextureIfAny(string name, ResourceNamespace resourceNamespace)
        {
            GLTextureType? texture = m_textureTracker.GetOnly(name, resourceNamespace);
            if (texture != null)
                DeleteTexture(texture, name, resourceNamespace);
        }

        private GLFontTexture<GLTextureType> CreateNewFont(Font font, string name)
        {
            GLFontTexture<GLTextureType> fontTexture = GenerateFont(font, name);

            DeleteOldFontIfAny(name);
            m_fonts[name] = fontTexture;

            return fontTexture;
        }

        private void DeleteOldFontIfAny(string name)
        {
            if (m_fonts.TryGetValue(name, out GLFontTexture<GLTextureType>? texture))
            {
                texture.Dispose();
                m_fonts.Remove(name);
            }
        }
    }
}