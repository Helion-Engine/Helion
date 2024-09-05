using System;
using System.Collections.Generic;
using Helion.Graphics;
using Helion.Graphics.Fonts;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
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

    private List<GLLegacyTexture> m_registeredTextures = new();

    public LegacyGLTextureManager(IConfig config, ArchiveCollection archiveCollection) : 
        base(config, archiveCollection)
    {
        ImageDrawInfoProvider = new GLLegacyImageDrawInfoProvider(this);

        Config.Render.Filter.Texture.OnChanged += HandleFilterChange;
        Config.Render.Anisotropy.OnChanged += HandleAnisotropyChange;
    }

    // Deals with setting parameters for config changes (filter and anistropy)
    public void RegisterTexture(GLLegacyTexture texture)
    {
        m_registeredTextures.Add(texture);
    }

    public void UnRegisterTexture(GLLegacyTexture texture)
    {
        m_registeredTextures.Remove(texture);
    }

    private void HandleFilterChange(object? sender, FilterType e)
    {
        UpdateTextureTrackerFilter(TextureTracker);
        UpdateTextureTrackerFilter(TextureTrackerClamp);

        foreach (GLLegacyTexture texture in m_registeredTextures)
        {
            texture.Bind();
            SetTextureFilter(texture.Target);
            texture.Unbind();
        }
    }

    private void UpdateTextureTrackerFilter(ResourceTracker<GLLegacyTexture> tracker)
    {
        foreach (var key in tracker.GetKeys())
        {
            if (key == ResourceNamespace.Sprites)
                continue;

            foreach (var texture in tracker.GetValues(key))
            {
                texture.Bind();
                SetTextureFilter(texture.Target);
                texture.Unbind();
            }
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

        foreach (GLLegacyTexture texture in m_registeredTextures)
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

        fixed (uint* pixelPtr = image.GetGlTexturePixels(ShaderVars.PaletteColorMode))
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

    public unsafe void ReUpload(GLLegacyTexture texture, Image image, uint[] imagePixels)
    {
        GL.BindTexture(texture.Target, texture.TextureId);

        fixed (uint* pixelPtr = imagePixels)
        {
            IntPtr ptr = new(pixelPtr);
            GL.TexSubImage2D(texture.Target, 0, 0, 0, image.Width, image.Height,
                PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, ptr);
        }

        GL.BindTexture(texture.Target, 0);
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
        GLLegacyTexture texture = new(textureId, name, image.Dimension, image.Offset, image.Namespace, TextureTarget.Texture2D, image.TransparentPixelCount(), image.BlankRowsFromBottom);
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
        GLLegacyTexture texture = GenerateTexture(font.Image, $"[FONT] {name}", ResourceNamespace.Fonts, TextureFlags.ClampX | TextureFlags.ClampY);
        GLFontTexture<GLLegacyTexture> fontTexture = new(texture, font);
        TextureTrackerClamp.Insert(name, ResourceNamespace.Fonts, texture);
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

    public void SetTextureFilter(TextureTarget targetType)
    {
        (int minFilter, int maxFilter) = FindFilterValues(Config.Render.Filter.Texture.Value);
        GL.TexParameter(targetType, TextureParameterName.TextureMinFilter, minFilter);
        GL.TexParameter(targetType, TextureParameterName.TextureMagFilter, maxFilter);
    }

    private (int minFilter, int maxFilter) FindFilterValues(FilterType filterType)
    {
        // Filtering must be nearest for colormap support
        int minFilter = (int)TextureMinFilter.Nearest;
        int magFilter = (int)TextureMagFilter.Nearest;

        if (ShaderVars.PaletteColorMode)
            return (minFilter, magFilter);

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

    public void SetAnisotropicFiltering(TextureTarget targetType)
    {
        // Anisotropy must be 1 for colormap support
        if (ShaderVars.PaletteColorMode)
        {
            GL.TexParameter(targetType, (TextureParameterName)All.TextureMaxAnisotropy, 1);
            return;
        }

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
