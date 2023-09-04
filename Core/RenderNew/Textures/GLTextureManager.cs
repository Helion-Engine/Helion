using System;
using Helion.Graphics;
using Helion.RenderNew.OpenGL.Textures;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Textures;
using Helion.Util.Configs;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.Textures;

public class GLTextureManager : IDisposable
{
    public readonly GLTexture2D NullTexture;
    public readonly GLTexture2D WhiteTexture;
    private readonly IConfig m_config;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly ResourceTracker<GLTexture2D?> m_loadedTextures = new();
    private bool m_disposed;

    public GLTextureManager(IConfig config, ArchiveCollection archiveCollection)
    {
        m_config = config;
        m_archiveCollection = archiveCollection;
        NullTexture = new("Null", Image.NullImage, TextureWrapMode.Repeat);
        WhiteTexture = new("White", Image.WhiteImage, TextureWrapMode.Repeat);
    }
    
    public bool Get(string name, ResourceNamespace resourceNamespace, out GLTexture2D texture)
    {
        // Try to get it first. If it's null, that means it doesn't exist and we don't
        // want to keep trying to create it.
        // Yes, I know, there are two lookups, but this should not be called often in
        // the optimized renderer or else something very wrong is being done.
        if (m_loadedTextures.Contains(name, resourceNamespace))
        {
            texture = m_loadedTextures.Get(name, resourceNamespace) ?? NullTexture;
            return ReferenceEquals(texture, NullTexture);
        }
        
        if (!m_archiveCollection.Textures.TryGet(name, resourceNamespace, out ResourceTexture? resourceTexture))
        {
            // We tried looking, it doesn't exist, never try again.
            m_loadedTextures.Insert(name, resourceNamespace, null);
            
            texture = NullTexture;
            return false;
        }

        texture = new($"'{resourceNamespace}' {name}", resourceTexture.Image, TextureWrapMode.Repeat);
        m_loadedTextures.Insert(name, resourceNamespace, texture);
        return true;
    }

    public void Dispose()
    {
        if (m_disposed)
            return;
        
        NullTexture.Dispose();
        WhiteTexture.Dispose();
        foreach (GLTexture2D texture in m_loadedTextures.GetValues())
            texture.Dispose();
        m_loadedTextures.Clear();

        m_disposed = true;
    }
}