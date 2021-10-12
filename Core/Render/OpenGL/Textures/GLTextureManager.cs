using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Graphics.Fonts;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Textures.Buffer;
using Helion.Render.OpenGL.Textures.Types;
using Helion.Render.OpenGL.Util;
using Helion.Resources;
using NLog;
using static Helion.Util.Assertion.Assert;
using Font = Helion.Graphics.Fonts.Font;
using Image = Helion.Graphics.Image;
using Texture = Helion.Resources.Textures.Texture;

namespace Helion.Render.OpenGL.Textures;

public class GLTextureManager : IRendererTextureManager
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public GLTextureHandle NullHandle { get; }
    public GLTextureHandle WhiteHandle { get; }
    public GLFontTexture NullFont { get; }
    private readonly IResources m_resources;
    private readonly GLTextureDataBuffer m_textureDataBuffer;
    private readonly List<AtlasGLTexture> m_textures = new() { new AtlasGLTexture("Atlas layer 0") };
    private readonly List<GLTextureHandle> m_handles = new();
    private readonly ResourceTracker<GLTextureHandle> m_handlesTracker = new();
    private readonly Dictionary<string, GLFontTexture> m_fontTextures = new(StringComparer.OrdinalIgnoreCase);
    private bool m_disposed;

    public GLTextureManager(IResources resources, GLTextureDataBuffer textureDataBuffer)
    {
        m_resources = resources;
        m_textureDataBuffer = textureDataBuffer;

        NullHandle = AddNullTexture();
        WhiteHandle = AddWhiteTexture();
        NullFont = AddNullFontTexture();
    }

    ~GLTextureManager()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    private GLTextureHandle AddNullTexture()
    {
        const string NullHandleName = "__NULL";

        GLTextureHandle? handle = AddImage(NullHandleName, Image.NullImage, Mipmap.Generate, Binding.Bind);
        return handle ?? throw new Exception("Should never fail to allocate the null texture");
    }

    private GLTextureHandle AddWhiteTexture()
    {
        const string WhiteHandleName = "__WHITE";

        Image image = new(16, 16, ImageType.Argb, fillColor: Color.White);
        GLTextureHandle? handle = AddImage(WhiteHandleName, image, Mipmap.Generate, Binding.Bind);
        return handle ?? throw new Exception("Should never fail to allocate the white texture");
    }

    private GLTextureHandle? AddImage(string name, Image image, Mipmap mipmap, Binding bind)
    {
        if (image.ImageType == ImageType.Palette)
            throw new Exception($"Image {name} must be converted to ARGB first before uploading to the GPU");

        Dimension neededDim = image.Dimension;
        Dimension maxDim = m_textures[0].Dimension;
        if (neededDim.Width > maxDim.Width || neededDim.Height > maxDim.Height)
            return null;

        for (int i = 0; i < m_textures.Count; i++)
        {
            AtlasGLTexture texture = m_textures[i];
            if (texture.TryUpload(image, out Box2I box, mipmap, bind))
                return CreateHandle(name, i, box, image, texture);
        }

        // Since we know it has to fit, but it didn't fit anywhere, then we
        // will make a new texture and use that, which must fit via precondition.
        AtlasGLTexture newTexture = new($"Atlas layer {m_textures.Count}");
        m_textures.Add(newTexture);

        if (!newTexture.TryUpload(image, out Box2I newBox, mipmap, bind))
        {
            Fail("Should never fail to upload an image when we allocated enough space for it (GL atlas texture)");
            return null;
        }

        return CreateHandle(name, m_textures.Count - 1, newBox, image, newTexture);
    }

    private GLTextureHandle CreateHandle(string name, int layerIndex, Box2I box, Image image,
        AtlasGLTexture atlasTexture)
    {
        int index = m_handles.Count;
        Vec2F uvFactor = atlasTexture.Dimension.Vector.Float;
        Vec2F min = box.Min.Float / uvFactor;
        Vec2F max = box.Max.Float / uvFactor;
        Box2F uvBox = new(min, max);

        GLTextureHandle handle = new(name, index, layerIndex, box, uvBox, image.Offset, atlasTexture);
        m_handles.Add(handle);
        m_handlesTracker.Insert(name, image.Namespace, handle);
        m_textureDataBuffer.SetTexture(handle);

        return handle;
    }

    private GLFontTexture AddNullFontTexture()
    {
        const string NullFontName = "Null font";

        Glyph glyph = new('?', Box2F.UnitBox, new Box2I((0, 0), Image.NullImage.Dimension.Vector));
        Dictionary<char, Glyph> glyphs = new() { ['?'] = glyph };
        Font font = new(NullFontName, glyphs, Image.NullImage);

        GLFontTexture fontTexture = new(NullFontName, font);
        m_fontTextures["NULL"] = fontTexture;

        return fontTexture;
    }

    private GLFontTexture AddFontTexture(string name, Font font)
    {
        GLFontTexture fontTexture = new(name, font);
        m_fontTextures[name] = fontTexture;
        return fontTexture;
    }

    public AtlasGLTexture GetAtlas(int index)
    {
        return m_textures[index];
    }

    public bool TryGet(string name, [NotNullWhen(true)] out IRenderableTextureHandle? handle,
        ResourceNamespace? specificNamespace = null)
    {
        GLTextureHandle texture = Get(name, specificNamespace ?? ResourceNamespace.Global);
        handle = texture;
        return !ReferenceEquals(texture, NullHandle);
    }

    /// <summary>
    /// Gets a texture with a name and priority namespace. If it cannot
    /// find one in the priority namespace, it will search others. If none
    /// can be found, the <see cref="NullHandle"/> is returned. If data is
    /// found for some existing texture in the resource texture manager, it
    /// will upload the texture data.
    /// </summary>
    /// <param name="name">The texture name, case insensitive.</param>
    /// <param name="priority">The first namespace to look at.</param>
    /// <returns>The texture handle, or the <see cref="NullHandle"/> if it
    /// cannot be found.</returns>
    public GLTextureHandle Get(string name, ResourceNamespace priority)
    {
        m_resources.Textures.TryGet(name, priority, out Texture? texture);
        return Get(texture, priority);
    }

    /// <summary>
    /// Looks up or creates a texture from an existing resource texture.
    /// </summary>
    /// <param name="texture">The texture to look up (or upload).</param>
    /// <param name="priority">The priority namespace to look up, or null
    /// if it does not matter. This is used in caching results, if our
    /// lookup fails and we pull from somewhere else. It is likely the
    /// case that the same call will be made again.</param>
    /// <returns>A texture handle.</returns>
    public GLTextureHandle Get(Texture? texture, ResourceNamespace? priority = null)
    {
        if (texture == null)
            return NullHandle;

        GLTextureHandle? handle = m_handlesTracker.Get(texture.Name, texture.Namespace);
        if (handle != null)
            return handle;

        Image image = texture.Image;
        GLTextureHandle? newHandle = AddImage(texture.Name, image, Mipmap.Generate, Binding.Bind);
        if (newHandle != null)
        {
            // If we grab it from another namespace, also track it in the
            // requested namespace because we know it doesn't exist and
            // instead can return hits quicker by caching the result.
            if (priority != null && priority != image.Namespace)
                m_handlesTracker.Insert(texture.Name, priority.Value, newHandle);

            return newHandle;
        }

        Log.Warn("Unable to allocate space for texture {Name} ({Dimension}, {Namespace})", texture.Name, image.Dimension, image.Namespace);
        return NullHandle;
    }

    /// <summary>
    /// Gets a font, or uploads it if it finds one and it has not been
    /// uploaded yet. If none can be found, the <see cref="NullFont"/> is
    /// returned.
    /// </summary>
    /// <param name="fontName">The font name, case insensitive.</param>
    /// <returns>The font handle, or <see cref="NullFont"/> if no font
    /// resource can be found.</returns>
    public GLFontTexture GetFont(string fontName)
    {
        if (m_fontTextures.TryGetValue(fontName, out GLFontTexture? fontTexture))
            return fontTexture;

        Font? font = m_resources.GetFont(fontName);
        if (font == null)
        {
            // Cache the null font for the font name provided so lookups
            // end earlier.
            m_fontTextures[fontName] = NullFont;
            return NullFont;
        }

        return AddFontTexture(fontName, font);
    }

    public bool TryGetFont(string font, out GLFontTexture fontTexture)
    {
        fontTexture = GetFont(font);
        return !ReferenceEquals(fontTexture, NullFont);
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

        m_handles.Clear();
        m_handlesTracker.Clear();

        foreach (GLFontTexture fontTexture in m_fontTextures.Values)
            fontTexture.Dispose();
        m_fontTextures.Clear();
        NullFont.Dispose();

        foreach (AtlasGLTexture texture in m_textures)
            texture.Dispose();
        m_textures.Clear();

        m_disposed = true;
    }
}

