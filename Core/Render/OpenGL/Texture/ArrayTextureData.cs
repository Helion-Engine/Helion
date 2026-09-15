using Helion.Util.Assertion;
using Helion.Util.Container;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Helion.Render.OpenGL.Texture;

internal readonly struct LookupKey(TextureFlags textureFlags, int textureHandle) : IEquatable<LookupKey>
{
    public readonly int Key1 = (int)textureFlags;
    public readonly int Key2 = textureHandle;

    public override int GetHashCode()
    {
        return Key1 + Key2 * 131072;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is LookupKey key)
            return key.Key1 == Key1 && key.Key2 == Key2;
        return false;
    }

    public bool Equals(LookupKey other)
    {
        return other.Key1 == Key1 && other.Key2 == Key2;
    }
}

internal sealed class ArrayTextureData<GLTextureType>(TextureContext context) where GLTextureType : GLTexture
{
    public readonly TextureContext Context = context;

    private readonly DynamicArray<GLTextureType[]> m_arraySubTextures = new(256);
    private readonly DynamicArray<GLTextureType> m_arrayTextures = new(256);
    private readonly DynamicArray<Resources.Texture> m_texturesForArrays = new(256);
    private readonly Dictionary<LookupKey, int> m_arrayTextureLookup = [];

    public void Add(GLTextureType arrayTexture, GLTextureType[] arraySubTextures, Span<Resources.Texture> textures, TextureFlags textureFlags)
    {
        Assert.Precondition(arraySubTextures.Length == textures.Length, "arraySubTextures != textures length");
        // Flip high bit to ensure no collisions
        var arrayTextureId = arrayTexture.TextureId | (1 << 30 - (int)Context);
        m_arrayTextures.Add(arrayTexture);
        m_arraySubTextures.Add(arraySubTextures);
        m_texturesForArrays.Add(textures);
        for (int i = 0; i < textures.Length; i++)
            m_arrayTextureLookup[new(textureFlags, textures[i].Index)] = arrayTextureId;
    }

    public int GetArrayTextureHandle(int textureHandle, TextureFlags textureFlags)
    {
        if (m_arrayTextureLookup.TryGetValue(new(textureFlags, textureHandle), out var handle))
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
        m_texturesForArrays.Clear();
    }
}
