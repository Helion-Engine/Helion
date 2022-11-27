using Helion;
using Helion.Graphics;
using Helion.Render;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Textures;

public class GLTextureManager : IDisposable
{
    public readonly GLTexture2D NullTexture;
    public readonly GLTexture2D WhiteTexture;
    private readonly IConfig m_config;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly Dictionary<int, GLTexture2D> m_textureidxToGLTexture = new();
    private readonly Dictionary<string, GLTexture2DFont> m_fonts = new(StringComparer.OrdinalIgnoreCase);
    private bool m_disposed;

    public GLTextureManager(IConfig config, ArchiveCollection archiveCollection)
    {
        m_config = config;
        m_archiveCollection = archiveCollection;
        NullTexture = new("Null image", Image.NullImage, TextureWrapMode.Repeat);
        WhiteTexture = new("White image", Image.WhiteImage, TextureWrapMode.Repeat);
    }

    ~GLTextureManager()
    {
        Dispose(false);
    }

    public bool TryGet(int index, out GLTexture2D glTexture)
    {
        if (m_textureidxToGLTexture.TryGetValue(index, out glTexture))
            return !ReferenceEquals(glTexture, NullTexture);

        // Try and find it, and track/return if found.
        Resources.Texture texture = m_archiveCollection.TextureManager.GetTexture(index);
        if (texture.Image != null)
        {
            glTexture = new($"{texture.Name} ({texture.Namespace})", texture.Image, GetWrapMode(texture), GetAnisotropy());
            m_textureidxToGLTexture[index] = NullTexture;
            return true;
        }
        
        // If we can't find it, store that it wasn't found.
        glTexture = NullTexture;
        m_textureidxToGLTexture[index] = NullTexture;
        return false;
    }

    private float? GetAnisotropy()
    {
        return m_config.Render.Anisotropy.Value >= 1 ? m_config.Render.Anisotropy.Value : null;
    }

    private TextureWrapMode GetWrapMode(Resources.Texture tex)
    {
        return tex.Namespace == ResourceNamespace.Sprites ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
    }

    public bool TryGetFont(string name, [NotNullWhen(true)] out GLTexture2DFont? glFont)
    {
        if (m_fonts.TryGetValue(name, out glFont))
            return true;

        // TODO: Find and make font if it exists.

        glFont = null;
        return false;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        foreach (GLTexture2D texture in m_textureidxToGLTexture.Values)
            texture.Dispose();
        m_textureidxToGLTexture.Clear();

        foreach (GLTexture2DFont font in m_fonts.Values)
            font.Dispose();
        m_fonts.Clear();

        WhiteTexture.Dispose();
        NullTexture.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
