using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Helion.Graphics;
using Helion.Graphics.Palettes;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Animdefs;
using Helion.Resources.Definitions.Animdefs.Textures;
using Helion.Resources.Definitions.Texture;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.Util.Loggers;
using Helion.World;

namespace Helion.Resources;

public class TextureManager : ITickable
{
    public const int NoTextureIndex = 0;
    const string ShittyTextureName = "AASHITTY";

    private readonly ArchiveCollection m_archiveCollection;
    private readonly List<Texture> m_textures;
    private readonly List<int> m_translations;
    private readonly Dictionary<string, Texture> m_textureLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Texture> m_flatLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Texture> m_patchLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Animation> m_animations = [];
    private readonly HashSet<int> m_animatedTextures = [];
    private readonly HashSet<int> m_processedEntityDefinitions = [];
    private readonly Dictionary<int, Entry[]> m_spriteIndexEntries = [];
    // Maps flat index to texture index
    private readonly Dictionary<int, int> m_skyTextureLookup = [];
    // Texture index to SkyDef
    private readonly Dictionary<int, SkyTransform> m_skyTransformLookup = [];
    private readonly List<SkyTransform> m_skyTransforms = [];
    private int m_skyIndex;
    private Texture? m_defaultSkyTexture;
    private readonly bool m_unitTest;
    private readonly bool m_cacheAllSprites;

    public DynamicArray<SpriteDefinition> SpriteDefinitions = new();

    public List<Animation> GetAnimations() => m_animations;

    public string SkyTextureName;
    public int NullCompatibilityTextureIndex = Constants.NullCompatibilityTextureIndex;

    public TextureManager(ArchiveCollection archiveCollection)
    {
        m_archiveCollection = archiveCollection;
        m_textures = [];
        m_translations = [];
        SkyTextureName = Constants.DefaultSkyTextureName;
    }

    public TextureManager(ArchiveCollection archiveCollection, bool cacheAllSprites, bool unitTest = false)
    {
        SkyTextureName = Constants.DefaultSkyTextureName;
        m_archiveCollection = archiveCollection;
        m_cacheAllSprites = cacheAllSprites;
        m_unitTest = unitTest;

        // Needs to be in ascending order for boom animated to work correctly, since it functions on lump index ranges.
        var flatEntries = m_archiveCollection.Entries.GetAllByNamespace(ResourceNamespace.Flats);
        int count = m_archiveCollection.Definitions.Textures.CountAll() + flatEntries.Count + 1;
        m_textures = new List<Texture>(count);

        var spriteEntries = m_archiveCollection.Entries.GetAllByNamespace(ResourceNamespace.Sprites);
        var spriteNames = spriteEntries.Where(entry => entry.Path.Name.Length > 3)
            .Select(x => x.Path.Name[0..4])
            .Distinct()
            .ToList();

        InitTextureArrays(m_archiveCollection.Definitions.Textures.GetValues(), flatEntries);

        m_translations = new List<int>(m_textures.Count);
        for (int i = 0; i < m_textures.Count; i++)
            m_translations.Add(i);

        InitAnimations();
        InitSwitches();
        MapSpriteIndexToEntries(spriteEntries, spriteNames);

        if (m_cacheAllSprites)
            InitSprites(spriteNames, spriteEntries);
    }

    private void MapSpriteIndexToEntries(List<Entry> spriteEntries, List<string> spriteNames)
    {
        foreach (var spriteName in spriteNames)
        {
            int spriteIndex = m_archiveCollection.EntityFrameTable.GetSpriteIndex(spriteName);
            m_spriteIndexEntries[spriteIndex] = spriteEntries.Where(entry => entry.Path.Name.StartsWith(spriteName)).ToArray();
        }
    }

    public void InitSprites(IWorld world)
    {
        if (m_cacheAllSprites)
            return;

        for (var entity = world.EntityManager.Head; entity != null; entity = entity.Next)
        {
            if (m_processedEntityDefinitions.Contains(entity.Definition.Id))
                continue;

            m_processedEntityDefinitions.Add(entity.Definition.Id);
            var def = entity.Definition;
            LoadSprite(def.SpawnState);
            LoadSprite(def.MissileState);
            LoadSprite(def.MeleeState);
            LoadSprite(def.DeathState);
            LoadSprite(def.XDeathState);
            LoadSprite(def.RaiseState);
            LoadSprite(def.SeeState);
            LoadSprite(def.PainState);
            LoadSprite(def.HealState);
        }

        m_processedEntityDefinitions.Clear();
    }

    private void InitSprites(List<string> spriteNames, List<Entry> spriteEntries)
    {
        SpriteDefinitions.Resize(m_archiveCollection.EntityFrameTable.SpriteIndexCount + 32);
        foreach (var spriteName in spriteNames)
        {
            var spriteDefEntries = spriteEntries.Where(entry => entry.Path.Name.StartsWith(spriteName)).ToList();
            int spriteIndex = m_archiveCollection.EntityFrameTable.GetSpriteIndex(spriteName);
            if (spriteIndex >= SpriteDefinitions.Capacity)
                SpriteDefinitions.Resize(spriteIndex + 32);

            GetSpriteDefinition(spriteIndex);
        }
    }

    private void LoadSprite(int? stateIndex)
    {
        if (!stateIndex.HasValue || stateIndex.Value < 0 || stateIndex.Value >= m_archiveCollection.EntityFrameTable.Frames.Count)
            return;

        var entityFrame = m_archiveCollection.EntityFrameTable.Frames[stateIndex.Value];
        int spriteIndex = m_archiveCollection.EntityFrameTable.GetSpriteIndex(entityFrame.Sprite);
        GetSpriteDefinition(spriteIndex);
    }

    public SpriteDefinition? GetSpriteDefinition(int spriteIndex)
    {
        if (spriteIndex >= SpriteDefinitions.Length || SpriteDefinitions[spriteIndex] == null)
        {
            var newSpriteDef = CreateSpriteDefinition(spriteIndex);
            return newSpriteDef;
        }

        return SpriteDefinitions.Data[spriteIndex];
    }

    public void SetSkyTexture(string skyTextureName)
    {
        if (skyTextureName.EqualsIgnoreCase(SkyTextureName))
            return;

        m_defaultSkyTexture = null;
        SkyTextureName = skyTextureName;
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

    public bool TryGetSkyTransform(int textureIndex, [NotNullWhen(true)] out SkyTransform? skyTransform)
    {
        return m_skyTransformLookup.TryGetValue(textureIndex, out skyTransform);
    }

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
    /// Checks if a texture is a sky.re
    /// </summary>
    /// <param name="texture">The texture to check.</param>
    public bool IsSkyTexture(int texture)
    {
        return texture == m_skyIndex || m_skyTextureLookup.ContainsKey(texture);
    }

    public bool IsDefaultSkyTexture(int texture)
    {
        return texture == m_skyIndex;
    }

    public bool GetSkyTextureFromFlat(int texture, out int skyTextureHandle)
    {
        return m_skyTextureLookup.TryGetValue(texture, out skyTextureHandle);
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
    public Texture GetTexture(string name, ResourceNamespace resourceNamespace, ResourceNamespace? priority = null)
    {
        if (name.Equals(Constants.NoTexture, StringComparison.OrdinalIgnoreCase))
            return m_textures[Constants.NoTextureIndex];

        if (m_unitTest)
            HandleUnitTestAdd(name, resourceNamespace, priority);

        Texture? texture;
        if (resourceNamespace == ResourceNamespace.Global)
        {
            if (priority == null || priority == ResourceNamespace.Textures)
            {
                if (m_textureLookup.TryGetValue(name, out texture))
                    return texture;
                if (m_flatLookup.TryGetValue(name, out texture))
                    return texture;
            }
            else
            {
                if (m_flatLookup.TryGetValue(name, out texture))
                    return texture;
                if (m_textureLookup.TryGetValue(name, out texture))
                    return texture;
            }
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

    public bool TryGetColormap(string? name, out Colormap? colormap)
    {
        if (string.IsNullOrEmpty(name))
        {
            colormap = null;
            return false;
        }
        return m_archiveCollection.Definitions.ColormapsLookup.TryGetValue(name, out colormap);
    }

    public bool IsTextureAnimated(int textureHandle) => m_animatedTextures.Contains(textureHandle);

    private void HandleUnitTestAdd(string name, ResourceNamespace resourceNamespace, ResourceNamespace? priority = null)
    {
        if (m_flatLookup.ContainsKey(name) || m_textureLookup.ContainsKey(name) || m_archiveCollection.Definitions.ColormapsLookup.ContainsKey(name))
            return;

        Texture? addedTexture = null;
        bool flats = resourceNamespace == ResourceNamespace.Flats || (priority != null && priority.Value == ResourceNamespace.Flats);
        // Have to set indices even if texture doesn't exist. Otherwise stair builder testing will break because it depends on the texture.
        if (flats && !m_flatLookup.ContainsKey(name))
        {
            addedTexture = new Texture(name, resourceNamespace, m_textures.Count);
            m_flatLookup[name] = addedTexture;
        }
        else if (!m_textureLookup.ContainsKey(name))
        {
            addedTexture = new Texture(name, resourceNamespace, m_textures.Count);
            m_textureLookup[name] = addedTexture;
        }

        if (addedTexture != null)
        {
            m_textures.Add(addedTexture);
            m_translations.Add(m_translations.Count);
        }
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

    public Texture GetNonAnimatedTexture(int index)
    {
        return m_textures[index];
    }

    public int GetTranslationIndex(int index)
    {
        return m_translations[index];
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
            Util.Assertion.Assert.Invariant(m_textures.Count > NullCompatibilityTextureIndex, "Invalid textures count");
            return m_textures[m_translations[NullCompatibilityTextureIndex]];
        }

        return m_textures[m_translations[index]];
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
                int newTexture = components[anim.AnimationIndex].TextureIndex;
                m_translations[anim.TranslationIndex] = newTexture;
                anim.Tics = 0;
            }
        }

        for (int i = 0; i < m_skyTransforms.Count; i++)
        {
            var transform = m_skyTransforms[i];
            transform.CurrentScroll += transform.Scroll;
        }
    }

    public void ResetAnimations()
    {
        for (int i = 0; i < m_animations.Count; i++)
        {
            Animation anim = m_animations[i];
            anim.AnimationIndex = 0;
            anim.Tics = 0;
        }

        for (int i = 0; i < m_textures.Count; i++)
            m_translations[i] = i;
    }

    private SpriteDefinition? CreateSpriteDefinition(int spriteIndex)
    {
        if (spriteIndex >= SpriteDefinitions.Capacity)
            SpriteDefinitions.Resize(spriteIndex + 32);

        if (!m_spriteIndexEntries.TryGetValue(spriteIndex, out var entries))
            return null;

        SpriteDefinitions.Data[spriteIndex] = new SpriteDefinition(entries, m_archiveCollection.ImageRetriever);
        return SpriteDefinitions.Data[spriteIndex];
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
            AnimatedSwitch sw = new(boomSwitch.Off);
            sw.StartTextureIndex = GetTexture(sw.Texture, ResourceNamespace.Global).Index;
            sw.On.Add(new AnimatedTextureComponent(boomSwitch.On, 0, 0, GetTexture(boomSwitch.On, ResourceNamespace.Global).Index));
            sw.Off.Add(new AnimatedTextureComponent(boomSwitch.Off, 0, 0, GetTexture(boomSwitch.Off, ResourceNamespace.Global).Index));
            m_archiveCollection.Definitions.Animdefs.AnimatedSwitches.Add(sw);
        }
    }

    private void InitAnimations()
    {
        InitRangeAnimations(m_archiveCollection.Definitions.BoomAnimated.AnimatedTextures);
        InitRangeAnimations(m_archiveCollection.Definitions.Animdefs.AnimatedRanges);
        InitAnimDefs();

        foreach (var anim in m_animations)
            m_animatedTextures.Add(anim.TranslationIndex);
    }

    private void InitAnimDefs()
    {
        foreach (var animTexture in m_archiveCollection.Definitions.Animdefs.AnimatedTextures)
        {
            if (animTexture.Components.Count == 0)
                continue;

            if (animTexture.Components[0].TextureIndex == Constants.NoTextureIndex)
            {
                HelionLog.Warn($"Bad animdefs texture {animTexture.Name}");
                continue;
            }

            foreach (var component in animTexture.Components)
            {
                if (component.ConfiguredTextureIndex != Constants.NoTextureIndex)
                {
                    Texture texture = GetTexture(component.ConfiguredTexture, animTexture.Namespace);
                    component.TextureIndex = texture.Index + component.ConfiguredTextureIndex - 1;
                    component.Texture = GetTexture(component.TextureIndex).Name;
                }

                component.TextureIndex = GetTexture(component.Texture, animTexture.Namespace).Index;
            }

            Animation animation = new(animTexture, animTexture.Components[0].TextureIndex);
            CreateComponentAnimations(animation);

            if (!HasAnimation(animTexture.Components[0].TextureIndex))
                m_animations.Add(animation);
        }
    }

    private void InitRangeAnimations(IEnumerable<IAnimatedRange> animatedRanges)
    {
        foreach (IAnimatedRange range in animatedRanges)
        {
            GetAnimatedRangeIndices(range, out int startIndex, out int endIndex);
            if (startIndex == Constants.NoTextureIndex || endIndex == Constants.NoTextureIndex)
                continue;

            if (endIndex <= startIndex)
                continue;

            Animation animation = new(new AnimatedTexture(GetTexture(startIndex).Name, false, range.Namespace), startIndex);
            m_animations.Add(animation);

            for (int i = startIndex; i <= endIndex; i++)
            {
                Texture texture = GetTexture(i);
                var component = new AnimatedTextureComponent(texture.Name,
                    range.MinTics, range.MaxTics, textureIndex: i);
                animation.AnimatedTexture.Components.Add(component);
            }

            CreateComponentAnimations(animation);
        }
    }

    private void GetAnimatedRangeIndices(IAnimatedRange range, out int startIndex, out int endIndex)
    {
        if (range.StartTextureIndex == -1)
            startIndex = GetTexture(range.StartTexture, range.Namespace).Index;
        else
            startIndex = range.StartTextureIndex;

        if (range.EndTextureIndex == -1)
            endIndex = GetTexture(range.EndTexture, range.Namespace).Index;
        else
            endIndex = range.EndTextureIndex;

        if (startIndex < Constants.NoTextureIndex)
            startIndex = Constants.NoTextureIndex;

        if (endIndex <= Constants.NoTextureIndex)
            endIndex = Constants.NoTextureIndex;
    }

    private void CreateComponentAnimations(Animation animation)
    {
        if (animation.TranslationIndex == Constants.NoTextureIndex)
            return;

        for (int i = 1; i < animation.AnimatedTexture.Components.Count; i++)
        {
            int nextAnimIndex = animation.AnimatedTexture.Components[i].TextureIndex;
            if (HasAnimation(nextAnimIndex))
                continue;

            Animation nextAnim = new(new AnimatedTexture(GetTexture(nextAnimIndex).Name, false,
                animation.AnimatedTexture.Namespace), nextAnimIndex);
            m_animations.Add(nextAnim);

            for (int j = 0; j < animation.AnimatedTexture.Components.Count; j++)
            {
                int index = (i + j) % animation.AnimatedTexture.Components.Count;
                var component = animation.AnimatedTexture.Components[index];
                nextAnim.AnimatedTexture.Components.Add(component);
            }
        }
    }

    private bool HasAnimation(int translationIndex) =>
        m_animations.Any(x => x.TranslationIndex == translationIndex);

    private void InitTextureArrays(List<TextureDefinition> textures, List<Entry> flatEntries)
    {
        m_textures.Add(new Texture(Constants.NoTexture, ResourceNamespace.Textures, Constants.NoTextureIndex));
        m_textureLookup[Constants.NoTexture] = m_textures[Constants.NoTextureIndex];

        // Need to force AASHITTY at NullCompatibilityTextureIndex (1)
        m_textures.Add(GetShittyTexture(textures));
        m_textureLookup[ShittyTextureName] = m_textures[Constants.NoTextureIndex];

        int index = Constants.NullCompatibilityTextureIndex + 1;
        foreach (TextureDefinition texture in textures.OrderBy(x => x.Index))
        {
            if (texture.Name.EqualsIgnoreCase(ShittyTextureName))
                continue;
            m_textures.Add(new Texture(texture.Name, texture.Namespace, index));
            m_textureLookup[texture.Name] = m_textures[index];
            index++;
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

            MapSky(flat, index);

            index++;
        }
    }

    private void MapSky(Entry flat, int textureIndex)
    {
        var skyDefinition = m_archiveCollection.Definitions.Id24SkyDefinition;
        if (!skyDefinition.FlatMapping.TryGetValue(flat.Path.Name, out var textureName))
            return;

        if (!m_textureLookup.TryGetValue(textureName, out var texture))
            return;

        for (int i = 0; i < skyDefinition.Data.Skies.Count; i++)
        {
            var sky = skyDefinition.Data.Skies[i];
            if (!sky.Name.Equals(texture.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            var skyTransform = SkyTransform.FromId24SkyDef(texture.Index, sky);
            m_skyTransforms.Add(skyTransform);
            m_skyTransformLookup[texture.Index] = skyTransform;
            break;
        }

        m_skyTextureLookup[textureIndex] = texture.Index;
        LoadTextureImage(texture.Index);
    }

    private Texture GetShittyTexture(List<TextureDefinition> textures)
    {
        // Load AASHITTY for information purposes - FloorRaiseByTexture needs it to emulate vanilla bug
        var texture = textures.FirstOrDefault(x => x.Name.EqualsIgnoreCase(ShittyTextureName));
        var ns = ResourceNamespace.Textures;
        var shittyTexture = new Texture(ShittyTextureName, ns, Constants.NullCompatibilityTextureIndex);
        shittyTexture.Image = texture == null ? Image.NullImage : m_archiveCollection.ImageRetriever.GetOnly(ShittyTextureName, ns);
        return shittyTexture;
    }

    private void LoadTextureImage(int textureIndex)
    {
        var texture = m_textures[textureIndex];
        if (texture.Image == null)
            texture.Image = m_archiveCollection.ImageRetriever.GetOnly(texture.Name, texture.Namespace);
    }

    public void SetSkyTexture()
    {
        string skyFlatName = m_archiveCollection.GameInfo.SkyFlatName;

        foreach (var texture in m_flatLookup.Values)
        {
            if (!texture.Name.Equals(skyFlatName, StringComparison.OrdinalIgnoreCase))
                continue;
            
            m_skyIndex = texture.Index;
            break;
        }
    }
}
