using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
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
    private bool m_disposed;

    public TextureHandle NullHandle => throw new("Null handle texture handle not implemented");
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
    
    public bool Get(string name, ResourceNamespace resourceNamespace, out TextureHandle handle)
    {
        // TODO
        handle = NullHandle;
        return false;
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
        
        // It doesn't exist, make space for it on the atlas and then upload it.
        if (!m_atlas.TryAllocate(texture.Image.Dimension, out Box2I area))
            throw new("Too many textures, cannot allocate on atlas");

        (Vec2F min, Vec2F max) = area.Float;
        Box2F uv = (min * m_atlas.UVFactor, max * m_atlas.UVFactor);
        TextureHandle handle = new(textureId, texture.Name, area, uv, texture.Namespace);
        m_textureHandles[textureId] = handle;
        
        m_textureAtlas.Bind();
        m_textureAtlas.Upload(texture, area.Min);
        Log.Error("[TODO] NEED TO DO MIPMAP UPDATE"); // TODO: Do this when we know more.
        m_textureAtlas.Unbind();
        
        return handle;
    }

    private void ExpandHandleListSizeIfNeeded(int textureId)
    {
        if (textureId < m_textureHandles.Count)
            return;
        
        for (int i = m_textureHandles.Count; i <= textureId; i++)
            m_textureHandles.Add(null!);
    }

    public void Dispose()
    {
        if (m_disposed)
            return;
        
        m_textureAtlas.Dispose();
        
        m_disposed = true;
    }
}