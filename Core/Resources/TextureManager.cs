using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Texture;
using Helion.Resources.Images;
using Helion.Util;

namespace Helion.Resources
{
    public class TextureManager
    {
        private readonly ArchiveImageRetriever m_imageRetriever;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly Texture[] m_textures;
        private readonly int[] m_translations;
        private readonly Dictionary<string, SpriteDefinition> m_spriteDefinitions = new Dictionary<string, SpriteDefinition>();
        private List<Animation> m_animations = new List<Animation>();
        private int m_skyIndex;

        // TODO - Maybe TextureManager shouldn't be an instance class - this is just to get us started.
        public static TextureManager Instance { get; private set; } = null!;

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

            foreach (var anim in m_animations)
            {
                if (textures.Contains(anim.TranslationIndex))
                {
                    foreach (var component in anim.AnimatedTexture.Components)
                        LoadTextureImage(component.TextureIndex);
                }
            }

            foreach (var sw in m_archiveCollection.Definitions.Animdefs.AnimatedSwitches)
            {
                LoadTextureImage(sw.StartTextureIndex);
                foreach (var component in sw.Components)
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
                texture = m_textures.FirstOrDefault(x => x.Name == name);
            else
                texture = m_textures.FirstOrDefault(x => x.Name == name && x.Namespace == resourceNamespace);

            if (texture == null)
                return m_textures[0];

            return texture;
        }

        /// <summary>
        /// Get a texture by index.
        /// </summary>
        /// <param name="index">The index of the texture.</param>
        /// <returns>Returns a texture at the given index. If the texture is animated it's current animation texture will be returned.</returns>
        public Texture GetTexture(int index)
        {
            return m_textures[m_translations[index]];
        }

        /// <summary>
        /// Get a sprite rotation.
        /// </summary>
        /// <param name="spriteName">Name of the sprite e.g. 'POSS' or 'SARG'.</param>
        /// <param name="frame">Sprite frame.</param>
        /// <param name="rotation">Rotation.</param>
        /// <returns>Returns a SpriteRotation if sprite name, frame, and rotation are valid. Otherwise null.</returns>
        public SpriteRotation? GetSpriteRotation(string spriteName, int frame, int rotation)
        {
            if (m_spriteDefinitions.TryGetValue(spriteName, out SpriteDefinition? spriteDef))
                return spriteDef.GetSpriteRotation(frame, rotation);
            return null;
        }

        public void Tick()
        {
            foreach (var anim in m_animations)
            {
                anim.Tics++;
                if (anim.Tics == anim.AnimatedTexture.Components[anim.AnimationIndex].MaxTicks)
                {
                    anim.AnimationIndex = ++anim.AnimationIndex % anim.AnimatedTexture.Components.Count;
                    m_translations[anim.TranslationIndex] = anim.AnimatedTexture.Components[anim.AnimationIndex].TextureIndex;
                    anim.Tics = 0;
                }
            }
        }

        private TextureManager(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
            m_imageRetriever = new ArchiveImageRetriever(archiveCollection);

            var flatEntries = m_archiveCollection.Entries.GetAllByNamespace(ResourceNamespace.Flats);
            int count = m_archiveCollection.Definitions.Textures.CountAll() + flatEntries.Count + 1;
            m_textures = new Texture[count];
            m_translations = new int[count];

            var spriteEntries = m_archiveCollection.Entries.GetAllByNamespace(ResourceNamespace.Sprites);
            var spriteNames = spriteEntries.Where(x => x.Path.Name.Length > 3).Select(x => x.Path.Name.Substring(0, 4)).Distinct().ToList();

            InitTextureArrays(m_archiveCollection.Definitions.Textures.GetValues(), flatEntries);
            InitAnimations();
            InitSwitches();
            InitSprites(spriteNames, spriteEntries);
        }

        private void InitSprites(List<string> spriteNames, List<Entry> spriteEntries)
        {
            foreach (var spriteName in spriteNames)
            {
                var spriteDefEntries = spriteEntries.Where(x => x.Path.Name.StartsWith(spriteName)).ToList();
                m_spriteDefinitions.Add(spriteName, new SpriteDefinition(spriteName, spriteDefEntries, m_imageRetriever));
            }
        }

        private void InitSwitches()
        {
            foreach (var sw in m_archiveCollection.Definitions.Animdefs.AnimatedSwitches)
            {
                sw.StartTextureIndex = GetTexture(sw.StartTexture, ResourceNamespace.Global).Index;
                foreach (var component in sw.Components)
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
                    m_animations.Add(new Animation(animTexture, component.TextureIndex));
                }
            }
        }

        private void InitTextureArrays(List<TextureDefinition> textures, List<Entry> flatEntries)
        {
            for (int i = 0; i < m_translations.Length; i++)
                m_translations[i] = i;

            m_textures[0] = new Texture(Constants.NoTexture, ResourceNamespace.Textures, Constants.NoTextureIndex);

            int index = 1;
            foreach (TextureDefinition texture in textures)
            {
                m_textures[index] = new Texture(texture.Name, texture.Namespace, index);
                index++;
            }

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
