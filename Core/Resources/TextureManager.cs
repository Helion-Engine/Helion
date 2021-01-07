﻿using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Animdefs.Textures;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.Definitions.Texture;
using Helion.Resources.Images;
using Helion.Util;

namespace Helion.Resources
{
    public class TextureManager : ITickable
    {
        private readonly List<Action<TextureManager>> m_notifyInitizalized = new List<Action<TextureManager>>();
        private readonly ArchiveImageRetriever m_imageRetriever;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly Texture[] m_textures;
        private readonly int[] m_translations;
        private readonly Dictionary<string, SpriteDefinition> m_spriteDefinitions = new Dictionary<string, SpriteDefinition>();
        private readonly List<Animation> m_animations = new List<Animation>();
        private int m_skyIndex;

        public static TextureManager Instance { get; private set; } = null!;

        public string SkyTextureName { get; set; }

        private TextureManager(ArchiveCollection archiveCollection, MapInfoDef mapInfoDef)
        {
            SkyTextureName = mapInfoDef.Sky1;
            m_archiveCollection = archiveCollection;
            m_imageRetriever = new ArchiveImageRetriever(archiveCollection);

            var flatEntries = m_archiveCollection.Entries.GetAllByNamespace(ResourceNamespace.Flats);
            int count = m_archiveCollection.Definitions.Textures.CountAll() + flatEntries.Count + 1;
            m_textures = new Texture[count];
            m_translations = new int[count];

            var spriteEntries = m_archiveCollection.Entries.GetAllByNamespace(ResourceNamespace.Sprites);
            var spriteNames = spriteEntries.Where(entry => entry.Path.Name.Length > 3)
                .Select(x => x.Path.Name.Substring(0, 4))
                .Distinct()
                .ToList();

            InitTextureArrays(m_archiveCollection.Definitions.Textures.GetValues(), flatEntries);
            InitAnimations();
            InitSwitches();
            InitSprites(spriteNames, spriteEntries);
        }

        public static void Init(ArchiveCollection archiveCollection, MapInfoDef mapInfoDef)
        {
            // This whole notify thing kind of sucks.
            // This exists because the SkySphere exists before this instance is created.
            // Since this gets recreated on a new map the SkySphereTexture needs to be 
            // notified that this is initializing for a new texture.
            List<Action<TextureManager>> notify = new();
            if (Instance != null)
                notify = Instance.m_notifyInitizalized;

            Instance = new TextureManager(archiveCollection, mapInfoDef);
            notify.ForEach(x => x.Invoke(Instance));
        }

        public void AddNotifyInitialized(Action<TextureManager> action)
        {
            m_notifyInitizalized.Add(action);
        }

        /// <summary>
        /// Loads the texture images.
        /// </summary>
        /// <param name="textures">List of texture indices to load.</param>
        public void LoadTextureImages(List<int> textures)
        {
            textures.ForEach(LoadTextureImage);

            foreach (Animation anim in m_animations)
            {
                if (textures.Contains(anim.TranslationIndex))
                {
                    foreach (var component in anim.AnimatedTexture.Components)
                        LoadTextureImage(component.TextureIndex);
                }
            }

            foreach (AnimatedSwitch animSwitch in m_archiveCollection.Definitions.Animdefs.AnimatedSwitches)
            {
                LoadTextureImage(animSwitch.StartTextureIndex);
                foreach (var component in animSwitch.On)
                    LoadTextureImage(component.TextureIndex);
                foreach (var component in animSwitch.Off)
                    LoadTextureImage(component.TextureIndex);
            }
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
        /// Get a texture by name and resource namespace.
        /// </summary>
        /// <param name="name">The name to search by.</param>
        /// <param name="resourceNamespace">The resource namespace to search by.</param>
        /// <returns>Returns the texture given the name and resource namespace.
        /// If not found the texture will be returned with Name = Constants.NoTexture and Index = Constants.NoTextureIndex.</returns>
        public Texture GetTexture(CIString name, ResourceNamespace resourceNamespace)
        {
            if (name == Constants.NoTexture)
                return m_textures[Constants.NoTextureIndex];

            Texture texture;
            if (resourceNamespace == ResourceNamespace.Global)
                texture = m_textures.FirstOrDefault(tex => tex.Name == name);
            else
                texture = m_textures.FirstOrDefault(tex => tex.Name == name && tex.Namespace == resourceNamespace);

            if (texture == null)
                return m_textures[Constants.NoTextureIndex];

            return texture;
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
                Util.Assertion.Assert.Invariant(m_textures.Length > 1, "Invalid textures count");
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

        private void InitSprites(List<string> spriteNames, List<Entry> spriteEntries)
        {
            foreach (var spriteName in spriteNames)
            {
                var spriteDefEntries = spriteEntries.Where(entry => entry.Path.Name.StartsWith(spriteName)).ToList();
                m_spriteDefinitions.Add(spriteName, new SpriteDefinition(spriteName, spriteDefEntries, m_imageRetriever));
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
        }

        private void InitAnimations()
        {
            foreach (var animTexture in m_archiveCollection.Definitions.Animdefs.AnimatedTextures)
            {
                foreach (var component in animTexture.Components)
                {
                    component.TextureIndex = GetTexture(component.Texture, ResourceNamespace.Global).Index;
                    if (component.TextureIndex != Constants.NoTextureIndex)
                        m_animations.Add(new Animation(animTexture, component.TextureIndex));
                }
            }
        }

        private void InitTextureArrays(List<TextureDefinition> textures, List<Entry> flatEntries)
        {
            for (int i = 0; i < m_translations.Length; i++)
                m_translations[i] = i;

            m_textures[Constants.NoTextureIndex] = new Texture(Constants.NoTexture, ResourceNamespace.Textures, Constants.NoTextureIndex);

            int index = Constants.NoTextureIndex + 1;
            foreach (TextureDefinition texture in textures)
            {
                m_textures[index] = new Texture(texture.Name, texture.Namespace, index);
                index++;
            }

            // Load AASHITTY for information purposes - FloorRaiseByTexture needs it to emulate vanilla bug
            if (m_textures.Length > 1)
            {
                Texture shitty = m_textures[1];
                shitty.Image = m_imageRetriever.GetOnly(shitty.Name, shitty.Namespace);
            }

            // TODO: When ZDoom's Textures lump becomes a thing, this will need updating.
            foreach (Entry flat in flatEntries)
            {
                m_textures[index] = new Texture(flat.Path.Name, ResourceNamespace.Flats, index);

                // TODO fix with MapInfo when implemented
                if (flat.Path.Name == Constants.SkyTexture)
                    m_skyIndex = index;

                index++;
            }
        }

        private void LoadTextureImage(int textureIndex)
        {
            var texture = m_textures[textureIndex];
            if (texture.Image == null)
                texture.Image = m_imageRetriever.GetOnly(texture.Name, texture.Namespace);
        }
    }
}
