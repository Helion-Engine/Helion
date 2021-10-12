using System.Collections.Generic;
using Helion.Graphics;

namespace Helion.Resources.Textures;

public class TextureManager : ITextureManager
{
    public const int NoTextureIndex = 0;

    public Texture NullTexture { get; }
    private readonly IResources m_resources;
    private readonly ResourceTracker<Texture> m_textureTracker = new();
    private readonly List<Texture> m_textures = new();
    private readonly List<int> m_translations = new();
    private readonly Dictionary<string, SpriteDefinition> m_spriteDefinitions = new();
    private readonly List<Animation> m_animations = new();
    private int m_skyIndex;

    public string SkyTextureName { get; set; } = "F_SKY1";

    public TextureManager(IResources resources)
    {
        m_resources = resources;
        NullTexture = CreateAndTrackTexture("Null", ResourceNamespace.Global, Image.NullImage);
    }

    /// <summary>
    /// Tries to get a texture by name and namespace.
    /// </summary>
    /// <param name="name">The texture name.</param>
    /// <param name="resourceNamespace">The desired resource namespace.</param>
    /// <param name="texture">The texture. If this returns false, this will
    /// still be a valid object, but point to the 'null texture' reference.</param>
    /// <returns>True if found, false if not.</returns>
    public bool TryGet(string name, ResourceNamespace resourceNamespace, out Texture? texture)
    {
        texture = GetTexture(name, resourceNamespace);
        return !texture.IsNullTexture;
    }

    /// <summary>
    /// Get a texture by name and resource namespace.
    /// </summary>
    /// <param name="name">The name to search by.</param>
    /// <param name="resourceNamespace">The resource namespace to search by.</param>
    /// <returns>Returns the texture given the name and resource namespace.
    /// If not found the texture will be returned with Name = Constants.NoTexture and Index = Constants.NoTextureIndex.</returns>
    public Texture GetTexture(string name, ResourceNamespace resourceNamespace)
    {
        Texture? texture = m_textureTracker.Get(name, resourceNamespace);
        if (texture != null)
        {
            // Cache the value so we don't return to it later on.
            if (texture.Namespace != resourceNamespace)
                m_textureTracker.Insert(name, resourceNamespace, texture);

            return texture;
        }

        Image? image = m_resources.ImageRetriever.Get(name, resourceNamespace);
        if (image == null)
        {
            // If there is no image for it, we should instead insert the null
            // texture so that lookups short-circuit.
            m_textureTracker.Insert(name, resourceNamespace, NullTexture);
            return NullTexture;
        }

        return CreateAndTrackTexture(name, resourceNamespace, image);
    }

    private Texture CreateAndTrackTexture(string name, ResourceNamespace resourceNamespace, Image image)
    {
        int index = m_textures.Count;
        Texture newTexture = new(index, name, image, resourceNamespace);
        m_textures.Add(newTexture);
        m_translations.Add(index);
        m_textureTracker.Insert(name, resourceNamespace, newTexture);

        // If the image was found from another namespace, we should track that
        // one as well as to save lookup time later on.
        if (resourceNamespace != image.Namespace)
            m_textureTracker.Insert(name, image.Namespace, newTexture);

        return newTexture;
    }

    /// <summary>
    /// Get a texture by index.
    /// </summary>
    /// <param name="index">The index of the texture.</param>
    /// <returns>Returns a texture at the given index. If the texture is
    /// animated it's current animation texture will be returned.</returns>
    public Texture GetTexture(int index)
    {
        return m_textures[m_translations[index]];
    }

    /// <summary>
    /// Get a texture by index, checks if the index is null and returns Doom's zero indexed texture (usually AASHITTY)
    /// This function is only intended to be used for vanilla compatibility like FloorRaiseByTexture
    /// </summary>
    /// <param name="index">The index of the texture.</param>
    /// <returns>Returns a texture at the given index. If the texture is
    /// animated it's current animation texture will be returned.</returns>
    public Texture GetNullCompatibilityTexture(int index)
    {
        int targetIndex = index != NoTextureIndex ? index : 1;
        int translatedIndex = m_translations[targetIndex];
        return m_textures[translatedIndex];
    }

    /// <summary>
    /// Get a sprite rotation.
    /// </summary>
    /// <param name="spriteName">Name of the sprite e.g. 'POSS' or 'SARG'.</param>
    /// <returns>Returns a SpriteDefinition if found by sprite name. Otherwise null.</returns>
    public SpriteDefinition? GetSpriteDefinition(string spriteName)
    {
        m_spriteDefinitions.TryGetValue(spriteName, out SpriteDefinition? spriteDef);
        return spriteDef;
    }

    public void Tick()
    {
        foreach (Animation anim in m_animations)
        {
            var components = anim.AnimatedTexture.Components;

            anim.Tics++;
            if (anim.Tics == components[anim.AnimationIndex].MaxTicks)
            {
                anim.AnimationIndex = ++anim.AnimationIndex % components.Count;
                m_translations[anim.TranslationIndex] = components[anim.AnimationIndex].TextureIndex;
                anim.Tics = 0;
            }
        }
    }
}

