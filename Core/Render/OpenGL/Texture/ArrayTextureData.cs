using Helion.Util.Assertion;
using Helion.Util.Container;
using OpenTK.Platform.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Render.OpenGL.Texture;

internal sealed class ArrayTextureData<GLTextureType>(TextureContext context) where GLTextureType : GLTexture
{
    public readonly TextureContext Context = context;

    private readonly DynamicArray<GLTextureType[]> m_arraySubTextures = new(256);
    private readonly DynamicArray<GLTextureType> m_arrayTextures = new(256);
    private readonly DynamicArray<Resources.Texture> m_texturesForArrays = new(256);
    private readonly Dictionary<int, int> m_arrayTextureLookup = [];
    private readonly Dictionary<int, int> m_arrayTextureLookupClamp = [];

    public void Add(GLTextureType arrayTexture, GLTextureType[] arraySubTextures, Span<Resources.Texture> textures, bool clamp)
    {
        Assert.Precondition(arraySubTextures.Length == textures.Length, "arraySubTextures != textures length");
        // Flip high bit to ensure no collisions
        var arrayTextureId = arrayTexture.TextureId | (1 << 30 - (int)Context);
        m_arrayTextures.Add(arrayTexture);
        m_arraySubTextures.Add(arraySubTextures);
        m_texturesForArrays.Add(textures);
        var lookup = clamp ? m_arrayTextureLookup : m_arrayTextureLookupClamp;
        for (int i = 0; i < textures.Length; i++)
            lookup[textures[i].Index] = arrayTextureId;
    }

    public int GetArrayTextureHandle(int textureHandle, bool clamp)
    {
        var lookup = clamp ? m_arrayTextureLookup : m_arrayTextureLookupClamp;
        if (lookup.TryGetValue(textureHandle, out var handle))
            return handle;
        return textureHandle;
    }

    public bool TryGetTexture(int textureIndex, [NotNullWhen(true)] out Resources.Texture? texture)
    {
        for (int i = 0; i < m_texturesForArrays.Length; i++)
        {
            var check = m_texturesForArrays.Data[i];
            if (check.Index == textureIndex)
            {
                texture = check;
                return true;
            }
        }

        texture = null;
        return false;
    }

    public void Destroy()
    {
        for (int i = 0; i < m_arrayTextures.Length; i++)
            m_arrayTextures[i].Dispose();

        for (int i = 0; i < m_texturesForArrays.Length; i++)
            m_texturesForArrays.Data[i].ClearGLTexture();

        m_arrayTextures.Clear();
        m_arraySubTextures.Clear();
        m_arrayTextureLookup.Clear();
        m_arrayTextureLookupClamp.Clear();
        m_texturesForArrays.Clear();
    }
}
