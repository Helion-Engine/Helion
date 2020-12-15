using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Animdefs.Switches;
using Helion.Resources.Definitions.Texture;
using Helion.Resources.Images;
using Helion.Util;

namespace Helion.Resources
{
    public class TextureManager : ITickable
    {
        private readonly ArchiveImageRetriever m_imageRetriever;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly Texture[] m_textures;
        private readonly int[] m_translations;
        private readonly Dictionary<string, SpriteDefinition> m_spriteDefinitions = new Dictionary<string, SpriteDefinition>();
        private readonly List<Animation> m_animations = new List<Animation>();
        private int m_skyIndex;

        public static TextureManager Instance { get; private set; } = null!;

        private TextureManager(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
            m_imageRetriever = new ArchiveImageRetriever(archiveCollection);

            var flatEntries = m_archiveCollection.Entries.GetAllByNamespace(Namespace.Flats);
            int count = m_archiveCollection.Definitions.Textures.CountAll() + flatEntries.Count + 1;
            m_textures = new Texture[count];
            m_translations = new int[count];

            var spriteEntries = m_archiveCollection.Entries.GetAllByNamespace(Namespace.Sprites);
            var spriteNames = spriteEntries.Where(entry => entry.Path.Name.Length > 3)
                .Select(x => x.Path.Name.Substring(0, 4))
                .Distinct()
                .ToList();

            InitTextureArrays(m_archiveCollection.Definitions.Textures.GetValues(), flatEntries);
            InitAnimations();
            InitSwitches();
            InitSprites(spriteNames, spriteEntries);
        }
        
        public static void Init(ArchiveCollection archiveCollection)
        {
            Instance = new TextureManager(archiveCollection);
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
                foreach (var component in animSwitch.Components)
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
        public Texture GetTexture(CIString name, Namespace resourceNamespace)
        {
            if (name == Constants.NoTexture)
                return m_textures[Constants.NoTextureIndex];

            Texture texture;
            if (resourceNamespace == Namespace.Global)
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
                sw.StartTextureIndex = GetTexture(sw.StartTexture, Namespace.Global).Index;
                foreach (var component in sw.Components)
                    component.TextureIndex = GetTexture(component.Texture, Namespace.Global).Index;
            }
        }

        private void InitAnimations()
        {
            foreach (var animTexture in m_archiveCollection.Definitions.Animdefs.AnimatedTextures)
            {
                foreach (var component in animTexture.Components)
                {
                    component.TextureIndex = GetTexture(component.Texture, Namespace.Global).Index;
                    if (component.TextureIndex != Constants.NoTextureIndex)
                        m_animations.Add(new Animation(animTexture, component.TextureIndex));
                }
            }
        }

        private void InitTextureArrays(List<TextureDefinition> textures, List<Entry> flatEntries)
        {
            for (int i = 0; i < m_translations.Length; i++)
                m_translations[i] = i;

            m_textures[Constants.NoTextureIndex] = new Texture(Constants.NoTexture, Namespace.Textures, Constants.NoTextureIndex);

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
                m_textures[index] = new Texture(flat.Path.Name, Namespace.Flats, index);
                
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
