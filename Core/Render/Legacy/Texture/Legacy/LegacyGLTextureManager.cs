using System.Drawing.Imaging;
using Helion.Graphics;
using Helion.Graphics.Fonts;
using Helion.Render.Common.Textures;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;
using Helion.Render.Legacy.Shared;
using Helion.Render.Legacy.Util;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Legacy.Texture.Legacy;

public class LegacyGLTextureManager : GLTextureManager<GLLegacyTexture>
{
    public override IImageDrawInfoProvider ImageDrawInfoProvider { get; }

    public LegacyGLTextureManager(IConfig config, GLCapabilities capabilities, IGLFunctions functions,
        ArchiveCollection archiveCollection)
        : base(config, capabilities, functions, archiveCollection)
    {
        ImageDrawInfoProvider = new GLLegacyImageDrawInfoProvider(this);

        // TODO: Listen for config changes to filtering/anisotropic.
    }

    ~LegacyGLTextureManager()
    {
        FailedToDispose(this);
        ReleaseUnmanagedResources();
    }

    public void UploadAndSetParameters(GLLegacyTexture texture, Image image, string name, ResourceNamespace resourceNamespace)
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
    /// <param name="image">The image that makes up this texture.</param>
    /// <param name="name">The name of the texture.</param>
    /// <param name="resourceNamespace">What namespace the texture is from.
    /// </param>
    /// <returns>A new texture.</returns>
    protected override GLLegacyTexture GenerateTexture(Image image, string name,
        ResourceNamespace resourceNamespace)
    {
        int textureId = gl.GenTexture();
        GLLegacyTexture texture = new(textureId, name, image.Dimension, image.Offset, image.Namespace, gl, TextureTargetType.Texture2D);
        UploadAndSetParameters(texture, image, name, resourceNamespace);

        return texture;
    }

    /// <summary>
    /// Creates a new font. The caller is responsible for disposing it.
    /// </summary>
    /// <param name="font">The font to make this from.</param>
    /// <param name="name">The name of the font.</param>
    /// <returns>A newly allocated font texture.</returns>
    protected override GLFontTexture<GLLegacyTexture> GenerateFont(Font font, string name)
    {
        GLLegacyTexture texture = GenerateTexture(font.Image, $"[FONT] {name}", ResourceNamespace.Fonts);
        GLFontTexture<GLLegacyTexture> fontTexture = new(texture, font);
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

        (int minFilter, int maxFilter) = FindFilterValues(Config.Render.Filter.Texture.Value);

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
        (int fontMinFilter, int fontMaxFilter) = FindFilterValues(Config.Render.Filter.Font.Value);
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
        if (Config.Render.Anisotropy <= 1)
            return;

        float value = Config.Render.Anisotropy.Value.Clamp(1, (int)Capabilities.Limits.MaxAnisotropy);
        gl.TexParameterF(targetType, TextureParameterFloatNameType.AnisotropyExt, value);
    }
}
