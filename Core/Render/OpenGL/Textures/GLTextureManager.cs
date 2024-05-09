using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Helion.Graphics;

namespace Helion.Render.OpenGL.Textures;

public class GLTextureManager : IDisposable
{
    public const int MaxSupportedTextures = 65536;
    
    public readonly GLImageHandle NullHandle;
    public readonly GLImageHandle WhiteHandle;
    public readonly GLBufferTexture TranslationTableBuffer = new("Translation table", new float[MaxSupportedTextures], false);
    public readonly GLBufferTexture TextureHandleBuffer = new("Texture handle", new float[MaxSupportedTextures], false);
    private readonly IConfig m_config;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly GLTexture2DArrayAtlas m_textures;
    private readonly DynamicArray<GLImageHandle?> m_textureIdxToPosition = new();
    private bool m_disposed;

    private float? Anisotropy => m_config.Render.Anisotropy.Value > 1 ? m_config.Render.Anisotropy.Value : null;

    public GLTextureManager(IConfig config, ArchiveCollection archiveCollection)
    {
        m_config = config;
        m_archiveCollection = archiveCollection;
        m_textures = new("Texture Atlas", (2048, 2048), 8, TextureWrapMode.Clamp, Anisotropy, true);
        NullHandle = m_textures.UploadImage(Image.NullImage);
        WhiteHandle = m_textures.UploadImage(Image.WhiteImage);
    }

    ~GLTextureManager()
    {
        Dispose(false);
    }

    // A convenience method for uploading every handle. Invoking this with textures that
    // are already uploaded is okay, since it will not cause duplicate uploading.
    public void UploadAll(HashSet<int> textureIndices)
    {
        foreach (int textureIndex in textureIndices)
            Get(textureIndex);
    }

    // Gets an image handle from the resource texture manager texture index. 
    public GLImageHandle Get(int index)
    {
        Debug.Assert(index >= 0, "Accessing a negative texture handle index");
        Debug.Assert(index < MaxSupportedTextures, "Likely accessing an incorrect texture index due to the large size");
        
        // TODO: Shouldn't this be done in increments? See what we did in entity-render-dev
        if (index >= m_textureIdxToPosition.Length)
            m_textureIdxToPosition.Resize(index);

        GLImageHandle? existingHandle = m_textureIdxToPosition[index];
        if (existingHandle != null)
            return existingHandle.Value;
        
        Resources.Texture texture = m_archiveCollection.TextureManager.GetTexture(index);

        if (texture.Index != Helion.Util.Constants.NoTextureIndex) 
            return UploadTexture(texture);
        
        m_textureIdxToPosition[index] = NullHandle;
        return NullHandle;

    }

    private GLImageHandle UploadTexture(Resources.Texture texture)
    {
        Debug.Assert(texture.Image != null, $"Did not read texture {texture.Name} ({texture.Namespace}) image data yet, cannot upload");

        m_textures.Bind();
        GLImageHandle handle = m_textures.UploadImage(texture.Image);
        m_textures.Unbind();
        m_textureIdxToPosition[texture.Index] = handle;
        
        // TODO: Update texture buffer #1
        // TODO: Update texture buffer #2
        throw new("TODO: Did not update texture buffer");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        m_textures.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
