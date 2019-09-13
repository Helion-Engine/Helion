using System.Drawing.Imaging;
using Helion.Graphics;
using Helion.Graphics.Fonts;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Texture.Fonts;
using Helion.Render.OpenGL.Util;
using Helion.Render.Shared;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Texture.Legacy
{
    public class LegacyGLTextureManager : GLTextureManager<GLLegacyTexture>
    {
        public override IImageDrawInfoProvider ImageDrawInfoProvider { get; }
        
        public LegacyGLTextureManager(Config config, GLCapabilities capabilities, IGLFunctions functions, 
            ArchiveCollection archiveCollection) 
            : base(config, capabilities, functions, archiveCollection)
        {
            ImageDrawInfoProvider = new GlLegacyImageDrawInfoProvider(this);
            
            // TODO: Listen for config changes to filtering/anisotropic.
        }
        
        ~LegacyGLTextureManager()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }
        
        public void UploadAndSetParameters(GLLegacyTexture texture, Image image, CIString name, ResourceNamespace resourceNamespace)
        {
            Precondition(image.Bitmap.PixelFormat == PixelFormat.Format32bppArgb, "Only support 32-bit ARGB images for uploading currently");
            
            gl.BindTexture(texture.TextureType, texture.TextureId);

            GLHelper.ObjectLabel(gl, Capabilities, ObjectLabelType.Texture, texture.TextureId, "Texture: " + name);

            var pixelArea = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
            var lockMode = ImageLockMode.ReadOnly;
            var format = PixelFormat.Format32bppArgb;
            var bitmapData = image.Bitmap.LockBits(pixelArea, lockMode, format);
            
            // Because the C# image format is 'ARGB', we can get it into the
            // RGBA format by doing a BGRA format and then reversing it.
            gl.TexImage2D(texture.TextureType, 0, PixelInternalFormatType.Rgba8, image.Dimension, 
                PixelFormatType.Bgra, PixelDataType.UnsignedInt8888Rev, bitmapData.Scan0);
            
            image.Bitmap.UnlockBits(bitmapData);

            Invariant(texture.TextureType == TextureTargetType.Texture2D, "Need to support non-2D textures for mipmapping");
            gl.GenerateMipmap(MipmapTargetType.Texture2D);
            SetTextureParameters(TextureTargetType.Texture2D, resourceNamespace);

            gl.BindTexture(texture.TextureType, 0);
        }

        /// <summary>
        /// Creates a new texture. The caller is responsible for disposing it.
        /// </summary>
        /// <param name="id">A unique ID for this texture.</param>
        /// <param name="image">The image that makes up this texture.</param>
        /// <param name="name">The name of the texture.</param>
        /// <param name="resourceNamespace">What namespace the texture is from.
        /// </param>
        /// <returns>A new texture.</returns>
        protected override GLLegacyTexture GenerateTexture(int id, Image image, CIString name, 
            ResourceNamespace resourceNamespace)
        {
            int textureId = gl.GenTexture();
            string textureName = $"{name} [{resourceNamespace}]";
            
            GLLegacyTexture texture = new GLLegacyTexture(id, textureId, textureName, image.Dimension, gl, TextureTargetType.Texture2D);
            UploadAndSetParameters(texture, image, name, resourceNamespace);
            
            return texture;
        }

        /// <summary>
        /// Creates a new font. The caller is responsible for disposing it.
        /// </summary>
        /// <param name="font">The font to make this from.</param>
        /// <param name="name">The name of the font.</param>
        /// <returns>A newly allocated font texture.</returns>
        protected override GLFontTexture<GLLegacyTexture> GenerateFont(Font font, CIString name)
        {
            (Image image, GLFontMetrics metrics) = GLFontGenerator.CreateFontAtlasFrom(font); 
            GLLegacyTexture texture = GenerateTexture(0, image, $"[FONT] {name}", ResourceNamespace.Fonts);
            GLFontTexture<GLLegacyTexture> fontTexture = new GLFontTexture<GLLegacyTexture>(texture, metrics);
            return fontTexture;
        }

        private void SetTextureParameters(TextureTargetType targetType, ResourceNamespace resourceNamespace)
        {
            // Sprites are a special case where we want to clamp to the edge.
            // This stops artifacts from forming.
            if (resourceNamespace == ResourceNamespace.Sprites)
            {
                HandleSpriteTextureParameters(targetType);
                return;
            }

            if (resourceNamespace == ResourceNamespace.Fonts)
            {
                HandleFontTextureParameters(targetType);
                return;
            }

            (int minFilter, int maxFilter) = FindFilterValues(Config.Engine.Render.TextureFilter);
            
            gl.TexParameter(targetType, TextureParameterNameType.MinFilter, minFilter);
            gl.TexParameter(targetType, TextureParameterNameType.MagFilter, maxFilter);
            gl.TexParameter(targetType, TextureParameterNameType.WrapS, (int)TextureWrapModeType.Repeat);
            gl.TexParameter(targetType, TextureParameterNameType.WrapT, (int)TextureWrapModeType.Repeat);
            SetAnisotropicFiltering(targetType);
        }

        private void HandleSpriteTextureParameters(TextureTargetType targetType)
        {
                gl.TexParameter(targetType, TextureParameterNameType.MinFilter, (int)TextureMinFilterType.Nearest);
                gl.TexParameter(targetType, TextureParameterNameType.MagFilter, (int)TextureMagFilterType.Nearest);
                gl.TexParameter(targetType, TextureParameterNameType.WrapS, (int)TextureWrapModeType.ClampToEdge);
                gl.TexParameter(targetType, TextureParameterNameType.WrapT, (int)TextureWrapModeType.ClampToEdge);
        }

        private void HandleFontTextureParameters(TextureTargetType targetType)
        {
            (int fontMinFilter, int fontMaxFilter) = FindFilterValues(Config.Engine.Render.FontFilter);
            gl.TexParameter(targetType, TextureParameterNameType.MinFilter, fontMinFilter);
            gl.TexParameter(targetType, TextureParameterNameType.MagFilter, fontMaxFilter);
            gl.TexParameter(targetType, TextureParameterNameType.WrapS, (int)TextureWrapModeType.ClampToEdge);
            gl.TexParameter(targetType, TextureParameterNameType.WrapT, (int)TextureWrapModeType.ClampToEdge);
        }

        private (int minFilter, int maxFilter) FindFilterValues(FilterType filterType)
        {
            int minFilter = (int)TextureMinFilterType.Nearest;
            int maxFilter = (int)TextureMagFilterType.Nearest;
            
            switch (filterType)
            {
            case FilterType.Nearest:
                // Already set as the default!
                break;
            case FilterType.Bilinear:
                minFilter = (int)TextureMinFilterType.Linear;
                maxFilter = (int)TextureMinFilterType.Linear;
                break;
            case FilterType.Trilinear:
                minFilter = (int)TextureMinFilterType.LinearMipmapLinear;
                maxFilter = (int)TextureMagFilterType.Linear;
                break;
            }

            return (minFilter, maxFilter);
        }

        private void SetAnisotropicFiltering(TextureTargetType targetType)
        {
            if (!Config.Engine.Render.Anisotropy.Enable)
                return;

            float value = (float)Config.Engine.Render.Anisotropy.Value;
            if (Config.Engine.Render.Anisotropy.UseMaxSupported)
                value = Capabilities.Limits.MaxAnisotropy;
            value = MathHelper.Clamp(value, 1.0f, Capabilities.Limits.MaxAnisotropy);

            gl.TexParameterF(targetType, TextureParameterFloatNameType.AnisotropyExt, value);
        }
    }
}