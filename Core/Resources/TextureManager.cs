using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Animdefs.Textures;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.Definitions.Texture;
using Helion.Util;

namespace Helion.Resources;

public class TextureManager : ITickable
{
    public const int NoTextureIndex = 0;

    private readonly ArchiveCollection m_archiveCollection;
    private readonly List<Texture> m_textures;
    private readonly List<int> m_translations;
    private readonly Dictionary<string, SpriteDefinition> m_spriteDefinitions = new();
    private readonly Dictionary<string, Texture> m_textureLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Texture> m_flatLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Texture> m_patchLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Animation> m_animations = new();
    private int m_skyIndex;
    private Texture? m_defaultSkyTexture;

    public static TextureManager Instance { get; private set; } = null!;

    public string SkyTextureName { get; set; }

    private TextureManager(ArchiveCollection archiveCollection, MapInfoDef? mapInfoDef = null)
    {
        m_archiveCollection = archiveCollection;
        SkyTextureName = mapInfoDef?.Sky1.Name ?? "SKY1";

        var flatEntries = m_archiveCollection.Entries.GetAllByNamespace(ResourceNamespace.Flats);
        int count = m_archiveCollection.Definitions.Textures.CountAll() + flatEntries.Count + 1;
        m_textures = new List<Texture>(count);
        m_translations = new List<int>(count);
        for (int i = 0; i < count; i++)
            m_translations.Add(i);

        var spriteEntries = m_archiveCollection.Entries.GetAllByNamespace(ResourceNamespace.Sprites);
        var spriteNames = spriteEntries.Where(entry => entry.Path.Name.Length > 3)
            .Select(x => x.Path.Name[0..4])
            .Distinct()
            .ToList();

        InitTextureArrays(m_archiveCollection.Definitions.Textures.GetValues(), flatEntries);
        InitAnimations();
        InitSwitches();
        InitSprites(spriteNames, spriteEntries);
    }

    public static void Init(ArchiveCollection archiveCollection, MapInfoDef? mapInfoDef = null)
    {
        Instance = new TextureManager(archiveCollection, mapInfoDef);
    }

    public Texture GetDefaultSkyTexture()
    {
        if (m_defaultSkyTexture != null)
            return m_defaultSkyTexture;

        m_defaultSkyTexture = GetTexture(SkyTextureName, ResourceNamespace.Global);
        if (m_defaultSkyTexture.Image == null)
            LoadTextureImage(m_defaultSkyTexture.Index);

        return m_defaultSkyTexture;
    }

    /// <summary>
    /// Loads the texture images.
    /// </summary>
    /// <param name="textures">List of texture indices to load.</param>
    public void LoadTextureImages(HashSet<int> textures)
    {
        foreach (int texture in textures)
            LoadTextureImage(texture);

        foreach (Animation anim in m_animations)
        {
            if (textures.Contains(anim.TranslationIndex))
                LoadAnimatedTextures(anim);
        }

        foreach (AnimatedSwitch animSwitch in m_archiveCollection.Definitions.Animdefs.AnimatedSwitches)
        {
            LoadTextureImage(animSwitch.StartTextureIndex);
            LoadAnimSwitchComponents(animSwitch.On);
            LoadAnimSwitchComponents(animSwitch.Off);
        }
    }

    private void LoadAnimSwitchComponents(IList<AnimatedTextureComponent> components)
    {
        foreach (var component in components)
        {
            LoadTextureImage(component.TextureIndex);
            var anim = m_animations.FirstOrDefault(x => x.TranslationIndex == component.TextureIndex);
            if (anim != null)
                LoadAnimatedTextures(anim);
        }
    }

    private void LoadAnimatedTextures(Animation anim)
    {
        foreach (var component in anim.AnimatedTexture.Components)
            LoadTextureImage(component.TextureIndex);
    }

    /// <summary>
    /// Checks if a texture is a sky.
    /// </summary>
    /// <param name="texture">The texture to check.</param>
    public bool IsSkyTexture(int texture)
    {
        return texture == m_skyIndex;
    }

    /// <summary>
    /// Tries to get a texture by name and namespace.
    /// </summary>
    /// <param name="name">The texture name.</param>
    /// <param name="resourceNamespace">The desired resource namespace.</param>
    /// <param name="texture">The texture. If this returns false, this will
    /// still be a valid object, but point to the 'null texture' reference.</param>
    /// <returns>True if found, false if not.</returns>
    public bool TryGet(string name, ResourceNamespace resourceNamespace, out Texture texture)
    {
        texture = GetTexture(name, resourceNamespace);
        return texture.Index == Constants.NoTextureIndex;
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
        if (name.Equals(Constants.NoTexture, StringComparison.OrdinalIgnoreCase))
            return m_textures[Constants.NoTextureIndex];

        Texture? texture;
        if (resourceNamespace == ResourceNamespace.Global)
        {
            if (m_textureLookup.TryGetValue(name, out texture))
                return texture;
            if (m_flatLookup.TryGetValue(name, out texture))
                return texture;
        }
        else
        {
            if (resourceNamespace == ResourceNamespace.Textures && m_textureLookup.TryGetValue(name, out texture))
                return texture;
            else if (resourceNamespace == ResourceNamespace.Flats && m_flatLookup.TryGetValue(name, out texture))
                return texture;
        }

        if (m_patchLookup.TryGetValue(name, out texture))
            return texture;

        // Doom allowed for direct patches to load...
        if (TryCreateTextureFromPatch(name, out texture))
            return texture!;

        return m_textures[Constants.NoTextureIndex];
    }

    private bool TryCreateTextureFromPatch(string name, out Texture? texture)
    {
        texture = null;
        var image = m_archiveCollection.ImageRetriever.GetOnly(name, ResourceNamespace.Global);
        if (image == null)
            return false;

        texture = new(name, ResourceNamespace.Textures, m_textures.Count);
        texture.Image = image;
        m_textures.Add(texture);
        m_translations.Add(m_translations.Count);
        m_patchLookup[name] = texture;
        return true;
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
        if (index == Constants.NoTextureIndex)
        {
            Util.Assertion.Assert.Invariant(m_textures.Count > 1, "Invalid textures count");
            return m_textures[m_translations[1]];
        }

        return m_textures[m_translations[index]];
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
        for (int i = 0; i < m_animations.Count; i++)
        {
            Animation anim = m_animations[i];
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

    private void InitSprites(List<string> spriteNames, List<Entry> spriteEntries)
    {
        foreach (var spriteName in spriteNames)
        {
            var spriteDefEntries = spriteEntries.Where(entry => entry.Path.Name.StartsWith(spriteName)).ToList();
            m_spriteDefinitions.Add(spriteName, new SpriteDefinition(spriteName, spriteDefEntries, m_archiveCollection.ImageRetriever));
        }
    }

    private void InitSwitches()
    {
        foreach (var sw in m_archiveCollection.Definitions.Animdefs.AnimatedSwitches)
        {
            sw.StartTextureIndex = GetTexture(sw.Texture, ResourceNamespace.Global).Index;
            foreach (var component in sw.On)
                component.TextureIndex = GetTexture(component.Texture, ResourceNamespace.Global).Index;
            foreach (var component in sw.Off)
                component.TextureIndex = GetTexture(component.Texture, ResourceNamespace.Global).Index;
        }

        foreach (var boomSwitch in m_archiveCollection.Definitions.BoomSwitches.Switches)
        {
            AnimatedSwitch sw = new AnimatedSwitch(boomSwitch.Off);
            sw.StartTextureIndex = GetTexture(sw.Texture, ResourceNamespace.Global).Index;
            sw.On.Add(new AnimatedTextureComponent(boomSwitch.On, 0, 0, GetTexture(boomSwitch.On, ResourceNamespace.Global).Index));
            sw.Off.Add(new AnimatedTextureComponent(boomSwitch.Off, 0, 0, GetTexture(boomSwitch.Off, ResourceNamespace.Global).Index));
            m_archiveCollection.Definitions.Animdefs.AnimatedSwitches.Add(sw);
        }
    }

    private void InitAnimations()
    {
        InitBoomAnimations();
        InitAnimDefs();
    }

    private void InitAnimDefs()
    {
        foreach (var animTexture in m_archiveCollection.Definitions.Animdefs.AnimatedTextures)
        {
            if (animTexture.Components.Count == 0)
                continue;

            foreach (var component in animTexture.Components)
                component.TextureIndex = GetTexture(component.Texture, ResourceNamespace.Global).Index;

            Animation animation = new Animation(animTexture, animTexture.Components[0].TextureIndex);
            m_animations.Add(animation);
            CreateComponentAnimations(animation);
        }
    }

    private void InitBoomAnimations()
    {
        foreach (var animTexture in m_archiveCollection.Definitions.BoomAnimated.AnimatedTextures)
        {
            ResourceNamespace resNamespace = animTexture.IsTexture ? ResourceNamespace.Textures : ResourceNamespace.Flats;
            int startIndex = GetTexture(animTexture.StartTexture, resNamespace).Index;
            if (startIndex == Constants.NoTextureIndex)
                continue;
            int endIndex = GetTexture(animTexture.EndTexture, resNamespace).Index;
            if (endIndex == Constants.NoTextureIndex)
                continue;

            if (endIndex <= startIndex)
                continue;

            Animation animation = new Animation(new AnimatedTexture(GetTexture(startIndex).Name.ToString(), false, resNamespace), startIndex);
            m_animations.Add(animation);

            for (int i = startIndex; i <= endIndex; i++)
            {
                Texture texture = GetTexture(i);
                var component = new AnimatedTextureComponent(texture.Name.ToString(),
                    animTexture.Tics, animTexture.Tics, textureIndex: i);
                animation.AnimatedTexture.Components.Add(component);
            }

            CreateComponentAnimations(animation);
        }
    }
    private void CreateComponentAnimations(Animation animation)
    {
        for (int i = 1; i < animation.AnimatedTexture.Components.Count; i++)
        {
            int nextAnimIndex = animation.AnimatedTexture.Components[i].TextureIndex;
            Animation nextAnim = new Animation(new AnimatedTexture(GetTexture(nextAnimIndex).Name.ToString(), false,
                animation.AnimatedTexture.Namespace), nextAnimIndex);
            m_animations.Add(nextAnim);

            for (int j = 0; j < animation.AnimatedTexture.Components.Count; j++)
            {
                int index = (i + j + 1) % animation.AnimatedTexture.Components.Count;
                var component = animation.AnimatedTexture.Components[index];
                nextAnim.AnimatedTexture.Components.Add(component);
            }
        }
    }

    private void InitTextureArrays(List<TextureDefinition> textures, List<Entry> flatEntries)
    {
        m_textures.Add(new Texture(Constants.NoTexture, ResourceNamespace.Textures, Constants.NoTextureIndex));
        m_textureLookup[Constants.NoTexture] = m_textures[Constants.NoTextureIndex];

        int index = Constants.NoTextureIndex + 1;
        foreach (TextureDefinition texture in textures)
        {
            m_textures.Add(new Texture(texture.Name, texture.Namespace, index));
            m_textureLookup[texture.Name] = m_textures[index];
            index++;
        }

        // Load AASHITTY for information purposes - FloorRaiseByTexture needs it to emulate vanilla bug
        if (m_textures.Count > 1)
        {
            Texture shitty = m_textures[1];
            shitty.Image = m_archiveCollection.ImageRetriever.GetOnly(shitty.Name, shitty.Namespace);
        }

        // TODO: When ZDoom's Textures lump becomes a thing, this will need updating.
        string skyFlatName = m_archiveCollection.GameInfo.SkyFlatName;
        foreach (Entry flat in flatEntries)
        {
            m_textures.Add(new Texture(flat.Path.Name, ResourceNamespace.Flats, index));
            m_flatLookup[flat.Path.Name] = m_textures[index];

            // TODO fix with MapInfo when implemented
            if (flat.Path.Name.Equals(skyFlatName, StringComparison.OrdinalIgnoreCase))
                m_skyIndex = index;

            index++;
        }
    }

    private void LoadTextureImage(int textureIndex)
    {
        var texture = m_textures[textureIndex];
        if (texture.Image == null)
            texture.Image = m_archiveCollection.ImageRetriever.GetOnly(texture.Name, texture.Namespace);
    }
}
