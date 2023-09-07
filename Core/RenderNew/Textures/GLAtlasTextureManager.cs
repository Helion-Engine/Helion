using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Graphics.Fonts;
using Helion.RenderNew.OpenGL.Textures;
using Helion.RenderNew.OpenGL.Util;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using NLog;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.Textures;

public class GLAtlasTextureManager : IDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    private static readonly Dimension InitialAtlasDimension = (8192, 8192);
    
    private readonly IConfig m_config;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly GLTexture2D m_textureAtlas;
    private readonly TextureAtlas m_atlas;
    private readonly List<TextureHandle> m_textureHandles = new();
    private readonly Dictionary<string, FontHandle> m_fontHandles = new(StringComparer.OrdinalIgnoreCase);
    private bool m_disposed;

    public TextureHandle NullHandle => Get(0);
    public TextureHandle WhiteHandle => throw new("White handle texture handle not implemented");

    public GLAtlasTextureManager(IConfig config, ArchiveCollection archiveCollection)
    {
        if (InitialAtlasDimension.Width > GLInfo.Limits.MaxTextureSize || InitialAtlasDimension.Height > GLInfo.Limits.MaxTextureSize)
            throw new($"GPU does not support image sizes of {InitialAtlasDimension.Width} (your max is {GLInfo.Limits.MaxTextureSize}). Speak to a developer to fix this.");
        
        m_config = config;
        m_archiveCollection = archiveCollection;
        m_atlas = new(InitialAtlasDimension);
        m_textureAtlas = new("Texture Atlas", InitialAtlasDimension, TextureWrapMode.Clamp);
    }

    /// <summary>
    /// Converts a <see cref="Texture"/> ID into a texture handle. Creates a new
    /// handle and uploads the texture otherwise.
    /// </summary>
    /// <param name="textureId">The resource texture ID.</param>
    /// <returns>The texture handle, or the "null texture handle" instance if the
    /// texture cannot be found.</returns>
    public TextureHandle Get(int textureId)
    {
        Texture resourceTexture = m_archiveCollection.TextureManager.GetTexture(textureId);
        return Get(resourceTexture);
    }
    
    public TextureHandle Get(string name, ResourceNamespace resourceNamespace)
    {
        Texture texture = m_archiveCollection.TextureManager.GetTexture(name, resourceNamespace);
        return Get(texture);
    }

    public TextureHandle Get(Texture texture)
    {
        if (texture.Image == null)
            throw new($"Trying to use texture ID {texture.Index} ({texture.Name}, {texture.Namespace}) when data not loaded");

        int textureId = texture.Index;
        
        // We always want to directly index into this list. Indices need to be
        // eventually offloaded to the shader as contiguous memory.
        ExpandHandleListSizeIfNeeded(textureId);
        
        TextureHandle existingHandle = m_textureHandles[textureId];
        
        // This also does a null check as well.
        if (existingHandle is not { IsNullTexture: true })
            return existingHandle;

        AllocateInAtlasOrThrow(texture.Image.Dimension, out Box2I area, out Box2F uv);
        UploadImageToAtlas(texture.Image, area);
        return CreateTextureHandle(textureId, texture.Name, area, uv, texture.Namespace);
    }

    private void AllocateInAtlasOrThrow(Dimension dimension, out Box2I area, out Box2F uv)
    {
        if (!m_atlas.TryAllocate(dimension, out area))
            throw new("Too many textures, cannot allocate on atlas");
        
        (Vec2F min, Vec2F max) = area.Float;
        uv = (min * m_atlas.UVFactor, max * m_atlas.UVFactor);
    }

    private void UploadImageToAtlas(Image image, Box2I area)
    {
        m_textureAtlas.Bind();
        m_textureAtlas.Upload(image, area.Min);
        Log.Error("[TODO] NEED TO DO MIPMAP UPDATE"); // TODO: Do this when we know more.
        m_textureAtlas.Unbind();
    }

    private TextureHandle CreateTextureHandle(int textureId, string textureName, Box2I area, Box2F uv, ResourceNamespace ns)
    {
        TextureHandle handle = new(textureId, textureName, area, uv, ns);
        m_textureHandles[textureId] = handle;
        return handle;
    }

    private void ExpandHandleListSizeIfNeeded(int textureId)
    {
        if (textureId < m_textureHandles.Count)
            return;
        
        for (int i = m_textureHandles.Count; i <= textureId; i++)
            m_textureHandles.Add(null!);
    }

    public bool TryGetFont(string name, out FontHandle? handle)
    {
        if (m_fontHandles.TryGetValue(name, out handle))
            return true;
        
        Font? font = m_archiveCollection.GetFont(name);
        if (font == null)
        {
            handle = null;
            return false;
        }

        Dictionary<char, TextureHandle> letterToTextureHandle = CreateLetterToTextureHandles(font);
        handle = new(font, letterToTextureHandle);
        m_fontHandles[name] = handle;
        return true;
    }

    private Dictionary<char, TextureHandle> CreateLetterToTextureHandles(Font font)
    {
        AllocateInAtlasOrThrow(font.Image.Dimension, out Box2I area, out Box2F _);
        UploadImageToAtlas(font.Image, area);
     
        Dictionary<char, TextureHandle> letterHandles = new();

        foreach ((char c, Glyph glyph) in font)
        {
            Box2I charArea = (area.Min + glyph.Area.Min, area.Min + glyph.Area.Max);
            Box2F charUV = (charArea.Min.Float * m_atlas.UVFactor, charArea.Max.Float * m_atlas.UVFactor);
            TextureHandle handle = new(TextureHandle.NullId, $"Font {font.Name} '{c}'", charArea, charUV, ResourceNamespace.Fonts);
            letterHandles[c] = handle;
        }

        return letterHandles;
    }

    public void Dispose()
    {
        if (m_disposed)
            return;
        
        m_textureAtlas.Dispose();
        
        m_disposed = true;
    }
}