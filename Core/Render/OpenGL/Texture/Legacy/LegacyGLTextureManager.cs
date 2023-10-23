using System;
using Helion.Graphics;
using Helion.Graphics.Fonts;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Util;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Texture.Legacy;

public class LegacyGLTextureManager : GLTextureManager<GLLegacyTexture>
{
    public override IImageDrawInfoProvider ImageDrawInfoProvider { get; }
    private bool m_disposed;

    public LegacyGLTextureManager(IConfig config, ArchiveCollection archiveCollection) : 
        base(config, archiveCollection)
    {
        ImageDrawInfoProvider = new GLLegacyImageDrawInfoProvider(this);

        Config.Render.Filter.Texture.OnChanged += HandleFilterChange;
        Config.Render.Anisotropy.OnChanged += HandleAnisotropyChange;
    }

    private void HandleFilterChange(object? sender, FilterType e)
    {
        foreach (GLLegacyTexture texture in TextureTracker.GetValues())
        {
            texture.Bind();
            SetTextureFilter(texture.Target);
            texture.Unbind();
        }
        
        foreach (GLLegacyTexture texture in TextureTrackerClamp.GetValues())
        {
            texture.Bind();
            SetTextureFilter(texture.Target);
            texture.Unbind();
        }
    }

    private void HandleAnisotropyChange(object? sender, int e)
    {
        foreach (GLLegacyTexture texture in TextureTracker.GetValues())
        {
            texture.Bind();
            SetAnisotropicFiltering(texture.Target);
            texture.Unbind();
        }
        
        foreach (GLLegacyTexture texture in TextureTrackerClamp.GetValues())
        {
            texture.Bind();
            SetAnisotropicFiltering(texture.Target);
            texture.Unbind();
        }
    }

    ~LegacyGLTextureManager()
    {
        FailedToDispose(this);
        Dispose();
    }

    public unsafe void UploadAndSetParameters(GLLegacyTexture texture, Image image, string name, ResourceNamespace resourceNamespace, TextureFlags flags)
    {
        GL.BindTexture(texture.Target, texture.TextureId);

        GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, texture.TextureId, $"Texture: {name} ({flags})");

        fixed (uint* pixelPtr = image.Pixels)
        {
            IntPtr ptr = new(pixelPtr);
            // Because the C# image format is 'ARGB', we can get it into the
            // RGBA format by doing a BGRA format and then reversing it.
            GL.TexImage2D(texture.Target, 0, PixelInternalFormat.Rgba8, image.Width, image.Height, 0,
                PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, ptr);
        }

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        SetTextureParameters(TextureTarget.Texture2D, resourceNamespace, flags);

        GL.BindTexture(texture.Target, 0);
        texture.Flags = flags;
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
        ResourceNamespace resourceNamespace, TextureFlags flags = TextureFlags.Default)
    {
        int textureId = GL.GenTexture();
        GLLegacyTexture texture = new(textureId, name, image.Dimension, image.Offset, image.Namespace, TextureTarget.Texture2D, image.TransparentPixelCount());
        UploadAndSetParameters(texture, image, name, resourceNamespace, flags);

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

    private void SetTextureParameters(TextureTarget targetType, ResourceNamespace resourceNamespace, TextureFlags flags)
    {
        if (resourceNamespace != ResourceNamespace.Sprites && resourceNamespace != ResourceNamespace.Graphics)
        {
            TextureWrapMode textureWrapS = flags.HasFlag(TextureFlags.ClampX) ? TextureWrapMode.ClampToEdge : TextureWrapMode.Repeat;
            TextureWrapMode textureWrapT = flags.HasFlag(TextureFlags.ClampY) ? TextureWrapMode.ClampToEdge : TextureWrapMode.Repeat;
            GL.TexParameter(targetType, TextureParameterName.TextureWrapS, (int)textureWrapS);
            GL.TexParameter(targetType, TextureParameterName.TextureWrapT, (int)textureWrapT);

            SetTextureFilter(targetType);
            SetAnisotropicFiltering(targetType);
            return;
        }

        // Sprites are a special case where we want to clamp to the edge.
        // This stops artifacts from forming.
        GL.TexParameter(targetType, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(targetType, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        if (resourceNamespace == ResourceNamespace.Sprites)
        {
            GL.TexParameter(targetType, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(targetType, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }
        else
        {
            SetTextureFilter(targetType);
        }
    }

    private void SetTextureFilter(TextureTarget targetType)
    {
        (int minFilter, int maxFilter) = FindFilterValues(Config.Render.Filter.Texture.Value);
        GL.TexParameter(targetType, TextureParameterName.TextureMinFilter, minFilter);
        GL.TexParameter(targetType, TextureParameterName.TextureMagFilter, maxFilter);
    }

    private void HandleSpriteTextureParameters(TextureTarget targetType)
    {
        GL.TexParameter(targetType, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(targetType, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(targetType, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(targetType, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
    }

    private void HandleFontTextureParameters(TextureTarget targetType)
    {
        (int fontMinFilter, int fontMaxFilter) = FindFilterValues(Config.Render.Filter.Font.Value);
        GL.TexParameter(targetType, TextureParameterName.TextureMinFilter, fontMinFilter);
        GL.TexParameter(targetType, TextureParameterName.TextureMagFilter, fontMaxFilter);
        GL.TexParameter(targetType, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(targetType, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
    }

    private (int minFilter, int maxFilter) FindFilterValues(FilterType filterType)
    {
        int minFilter = (int)TextureMinFilter.Nearest;
        int magFilter = (int)TextureMagFilter.Nearest;

        switch (filterType)
        {
        case FilterType.Nearest:
            // Already set as the default!
            break;
        case FilterType.Bilinear:
            minFilter = (int)TextureMinFilter.Linear;
            magFilter = (int)TextureMagFilter.Linear;
            break;
        case FilterType.Trilinear:
            minFilter = (int)TextureMinFilter.LinearMipmapLinear;
            magFilter = (int)TextureMagFilter.Linear;
            break;
        }

        return (minFilter, magFilter);
    }

    private void SetAnisotropicFiltering(TextureTarget targetType)
    {
        if (Config.Render.Anisotropy <= 1)
            return;

        float value = Config.Render.Anisotropy.Value.Clamp(1, (int)GLLimits.MaxAnisotropy);
        GL.TexParameter(targetType, (TextureParameterName)All.TextureMaxAnisotropy, value);
    }

    private void PerformDispose()
    {
        if (m_disposed)
            return;
        
        Config.Render.Filter.Texture.OnChanged -= HandleFilterChange;
        Config.Render.Anisotropy.OnChanged -= HandleAnisotropyChange;

        m_disposed = true;
    }

    public override void Dispose()
    {
        base.Dispose();
        PerformDispose();
    }
}
