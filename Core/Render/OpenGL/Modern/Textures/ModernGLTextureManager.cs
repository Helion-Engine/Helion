using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using Helion.Graphics;
using Helion.Graphics.Fonts;
using Helion.Render.Common;
using Helion.Render.OpenGL.Capabilities;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using OpenTK.Graphics.OpenGL4;
using static Helion.Util.Assertion.Assert;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace Helion.Render.OpenGL.Modern.Textures
{
    public class ModernGLTextureManager : IDisposable
    {
        public readonly ModernGLTexture NullTexture;
        private readonly GLCapabilities m_capabilities; 
        private readonly Config m_config;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly Dictionary<string, ModernGLFontTexture> m_fonts = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Texture, ModernGLTexture> m_textures = new();
        private readonly ResourceTracker<ModernGLTexture> m_lookupTable = new();
        private bool m_disposed;
        
        public TextureManager ArchiveTextureManager => TextureManager.Instance; 

        public ModernGLTextureManager(GLCapabilities capabilities, Config config, ArchiveCollection archiveCollection)
        {
            m_capabilities = capabilities;
            m_config = config;
            m_archiveCollection = archiveCollection;
            NullTexture = CreateNullTexture();
            
            // TODO: Listen for config changes
        }

        ~ModernGLTextureManager()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        /// <summary>
        /// Looks up a texture that is mapped to the provided texture. If none
        /// is found, will create a new one and upload the data so it can be
        /// immediately used.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <returns>A GL texture for rendering with.</returns>
        public ModernGLTexture Get(Texture texture)
        {
            if (m_textures.TryGetValue(texture, out ModernGLTexture? existingTexture))
                return existingTexture;

            if (texture.Image == null)
                return NullTexture;
            
            return CreateAndTrackTexture(texture, texture.Image);
        }
        
        /// <summary>
        /// Gets the font for the texture name provided.
        /// </summary>
        /// <param name="name">The font name (case insensitive).</param>
        /// <returns>The font texture, or null if no texture exists in the
        /// resources.</returns>
        public ModernGLFontTexture? GetFont(string name)
        {
            if (m_fonts.TryGetValue(name, out ModernGLFontTexture? existingTexture))
                return existingTexture;

            IFont? font = m_archiveCollection.GetFont(name);
            if (font == null)
                return null;

            return CreateAndTrackFontTexture(name, font);
        }

        /// <summary>
        /// Creates a completely new, untracked texture. It is the caller's
        /// responsibility to free this manually. Failing to do so is a memory
        /// leak.
        /// </summary>
        /// <param name="texture">The texture to make a handle from.</param>
        /// <param name="target">The GL texture target type.</param>
        /// <returns>A new handle, or null if it cannot be made.</returns>
        public ModernGLTextureHandle? CustomAllocate(Texture texture, TextureTarget target)
        {
            if (texture.Image == null)
                return null;
            
            ModernGLTexture glTexture = GenerateTexture(texture.Image, target, texture.Name);
            ModernGLTextureHandle handle = new(glTexture);
            return handle;
        }

        private ModernGLTexture CreateAndTrackTexture(Texture texture, Image image)
        {
            Precondition(!m_textures.ContainsKey(texture), $"Accidentally overwriting GL texture {texture.Name}");
            
            ModernGLTexture glTexture = GenerateTexture(image, TextureTarget.Texture2D, texture.Name);
            
            m_textures[texture] = glTexture;
            m_lookupTable.Insert(texture.Name, texture.Namespace, glTexture);

            return glTexture;
        }

        private ModernGLTexture CreateNullTexture()
        {
            Image image = ImageHelper.CreateNullImage();
            return GenerateTexture(image, TextureTarget.Texture2D, "NULL");
        }

        private ModernGLTexture GenerateTexture(Image image, TextureTarget target, string textureName)
        {
            ModernGLTexture texture = new(textureName, image, target);
            SetTextureDataAndProperties(texture, textureName, image, target);
            return texture;
        }

        private ModernGLFontTexture CreateAndTrackFontTexture(string name, IFont font)
        {
            ModernGLFontTexture texture = new(name, font);
            SetTextureDataAndProperties(texture, $"(Font) {font.Name}", font.Image, TextureTarget.Texture2D);
            m_fonts[name] = texture;

            return texture;
        }

        private void SetTextureDataAndProperties(ModernGLTexture texture, string debugName, Image image, 
            TextureTarget target)
        {
            texture.BindAnd(() =>
            {
                texture.SetDebugLabel(debugName);
                UploadTexturePixels(image, target);
                GenerateMipmap(target);
                SetTextureParameters(target, image.Metadata.Namespace);
                texture.InitializeBindlessHandle(true);
            });
        }

        private static void UploadTexturePixels(Image image, TextureTarget target)
        {
            Precondition(image.Bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb, "Only support 32-bit ARGB images for uploading currently");

            var pixelArea = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
            var lockMode = ImageLockMode.ReadOnly;
            var format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            var bitmapData = image.Bitmap.LockBits(pixelArea, lockMode, format);
            
            // Because the C# image format is 'ARGB', we can get it into the
            // RGBA format by doing a BGRA format and then reversing it.
            (int w, int h) = image.Dimension;
            GL.TexImage2D(target, 0, PixelInternalFormat.Rgba8, w, h, 0, PixelFormat.Bgra,
                PixelType.UnsignedInt8888Reversed, bitmapData.Scan0);
            
            image.Bitmap.UnlockBits(bitmapData);
        }

        private static void GenerateMipmap(TextureTarget textureTarget)
        {
            Precondition(textureTarget == TextureTarget.Texture2D, "Need to support non-2D textures for mipmapping");

            GenerateMipmapTarget target = textureTarget switch
            {
                TextureTarget.Texture2D => GenerateMipmapTarget.Texture2D,
                _ => GenerateMipmapTarget.Texture2D 
            };
            
            GL.GenerateMipmap(target);
        }

        private void SetTextureParameters(TextureTarget target, ResourceNamespace resourceNamespace)
        {
            if (resourceNamespace == ResourceNamespace.Sprites)
                SetSpriteTextureParameters(target);
            else if (resourceNamespace == ResourceNamespace.Fonts)
                SetFontTextureParameters(target);
            else
                SetStandardTextureParameters(target);
        }

        private void SetStandardTextureParameters(TextureTarget target)
        {
            (int minFilter, int magFilter) = FindFilterValues(m_config.Render.TextureFilter);
            
            GL.TexParameter(target, TextureParameterName.TextureMinFilter, minFilter);
            GL.TexParameter(target, TextureParameterName.TextureMagFilter, magFilter);
            GL.TexParameter(target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            SetAnisotropicFiltering(target);
        }

        private void SetSpriteTextureParameters(TextureTarget target)
        {
            GL.TexParameter(target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        }

        private void SetFontTextureParameters(TextureTarget target)
        {
            (int fontMinFilter, int fontMagFilter) = FindFilterValues(m_config.Render.FontFilter);
            
            GL.TexParameter(target, TextureParameterName.TextureMinFilter, fontMinFilter);
            GL.TexParameter(target, TextureParameterName.TextureMagFilter, fontMagFilter);
            GL.TexParameter(target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        }
        
        private (int minFilter, int magFilter) FindFilterValues(FilterType filterType)
        {
            TextureMinFilter minFilter = filterType switch
            {
                FilterType.Bilinear => TextureMinFilter.Linear,
                FilterType.Trilinear => TextureMinFilter.LinearMipmapLinear,
                _ => TextureMinFilter.Nearest
            };
            
            TextureMagFilter magFilter = filterType switch
            {
                FilterType.Bilinear => TextureMagFilter.Linear,
                FilterType.Trilinear => TextureMagFilter.Linear,
                _ => TextureMagFilter.Nearest
            };

            return ((int)minFilter, (int)magFilter);
        }

        private void SetAnisotropicFiltering(TextureTarget target)
        {
            if (!m_config.Render.Anisotropy.Enable)
                return;

            float value = m_config.Render.Anisotropy.UseMaxSupported ?
                m_capabilities.Limits.MaxAnisotropy :
                (float)m_config.Render.Anisotropy.Value;
            
            value = value.Clamp(1.0f, m_capabilities.Limits.MaxAnisotropy);
            GL.TexParameter(target, (TextureParameterName)0x84FE, value);
        }
        
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;
            
            // Note that this data structure is only for weak references.
            m_lookupTable.Clear();
            
            foreach (ModernGLFontTexture fontTexture in m_fonts.Values)
                fontTexture.Dispose();
            m_fonts.Clear();

            foreach (ModernGLTexture texture in m_textures.Values)
                texture.Dispose();
            m_textures.Clear();

            m_disposed = true;
        }
    }
}
