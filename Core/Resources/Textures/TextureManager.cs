using System.Collections.Generic;
using Helion.Graphics;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Images;
using Helion.Util;

namespace Helion.Resources.Textures
{
    public class TextureManager : ITextureManager
    {
        public const int NoTextureIndex = 0;
        
        private readonly IResources m_resources;
        private readonly ResourceTracker<Texture> m_textureTracker = new();
        private readonly List<Texture> m_textures = new();
        private readonly List<int> m_translations = new();
        private readonly Dictionary<string, SpriteDefinition> m_spriteDefinitions = new();
        private readonly List<Animation> m_animations = new();
        private int m_skyIndex;

        public string SkyTextureName { get; set; } = "F_SKY1";

        public TextureManager(ArchiveCollection archiveCollection)
        {
            m_resources = archiveCollection;
        }

        public void PreloadSprites()
        {
            foreach (Entry entry in m_resources.GetEntriesByNamespace(ResourceNamespace.Sprites))
                GetTexture(entry.Path.Name, ResourceNamespace.Sprites);
        }

        public int CalculateTotalTextureCount()
        {
            // TODO: This is not correct, and is only a placeholder.
            return m_textures.Count;
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
            return texture != null;
        }
        
        /// <summary>
        /// Get a texture by name and resource namespace.
        /// </summary>
        /// <param name="name">The name to search by.</param>
        /// <param name="resourceNamespace">The resource namespace to search by.</param>
        /// <returns>Returns the texture given the name and resource namespace.
        /// If not found the texture will be returned with Name = Constants.NoTexture and Index = Constants.NoTextureIndex.</returns>
        public Texture? GetTexture(string name, ResourceNamespace resourceNamespace)
        {
            Texture? texture = m_textureTracker.GetOnly(name, resourceNamespace);
            if (texture != null)
                return texture;
            
            texture = m_textureTracker.Get(name, resourceNamespace);
            if (texture != null)
            {
                // Cache the value so we don't return to it later on.
                m_textureTracker.Insert(name, resourceNamespace, texture);
                return texture;
            }
            
            Image? image = m_resources.ImageRetriever.Get(name, resourceNamespace);
            if (image == null)
                return null;

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
            int targetIndex = index != Constants.NoTextureIndex ? index : 1;
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
        
        // /// <summary>
        // /// Loads the texture images.
        // /// </summary>
        // /// <param name="textures">List of texture indices to load.</param>
        // public void LoadTextureImages(HashSet<int> textures)
        // {
        //     foreach (int texture in textures)
        //         LoadTextureImage(texture);
        //
        //     foreach (Animation anim in m_animations)
        //     {
        //         if (textures.Contains(anim.TranslationIndex))
        //             LoadAnimatedTextures(anim);
        //     }
        //
        //     foreach (AnimatedSwitch animSwitch in m_archiveCollection.Definitions.Animdefs.AnimatedSwitches)
        //     {
        //         LoadTextureImage(animSwitch.StartTextureIndex);
        //         LoadAnimSwitchComponents(animSwitch.On);
        //         LoadAnimSwitchComponents(animSwitch.Off);
        //     }
        // }
        //
        // private void LoadAnimSwitchComponents(IList<AnimatedTextureComponent> components)
        // {
        //     foreach (var component in components)
        //     {
        //         LoadTextureImage(component.TextureIndex);
        //         var anim = m_animations.FirstOrDefault(x => x.TranslationIndex == component.TextureIndex);
        //         if (anim != null)
        //             LoadAnimatedTextures(anim);
        //     }
        // }
        //
        // private void LoadAnimatedTextures(Animation anim)
        // {
        //     foreach (var component in anim.AnimatedTexture.Components)
        //         LoadTextureImage(component.TextureIndex);
        // }
        //
        // private void InitSprites(List<string> spriteNames, List<Entry> spriteEntries)
        // {
        //     foreach (var spriteName in spriteNames)
        //     {
        //         var spriteDefEntries = spriteEntries.Where(entry => entry.Path.Name.StartsWith(spriteName)).ToList();
        //         m_spriteDefinitions.Add(spriteName, new SpriteDefinition(spriteName, spriteDefEntries, m_imageRetriever));
        //     }
        // }
        //
        // private void InitSwitches()
        // {
        //     foreach (var sw in m_resources.Animdefs.AnimatedSwitches)
        //     {
        //         sw.StartTextureIndex = GetTexture(sw.Texture, ResourceNamespace.Global).Index;
        //         foreach (var component in sw.On)
        //             component.TextureIndex = GetTexture(component.Texture, ResourceNamespace.Global).Index;
        //         foreach (var component in sw.Off)
        //             component.TextureIndex = GetTexture(component.Texture, ResourceNamespace.Global).Index;
        //     }
        //     
        //     foreach (var boomSwitch in m_resources.BoomSwitches.Switches)
        //     {
        //         AnimatedSwitch sw = new AnimatedSwitch(boomSwitch.Off);
        //         sw.StartTextureIndex = GetTexture(sw.Texture, ResourceNamespace.Global).Index;
        //         sw.On.Add(new AnimatedTextureComponent(boomSwitch.On, 0, 0, GetTexture(boomSwitch.On, ResourceNamespace.Global).Index));
        //         sw.Off.Add(new AnimatedTextureComponent(boomSwitch.Off, 0, 0, GetTexture(boomSwitch.Off, ResourceNamespace.Global).Index));
        //         m_resources.Animdefs.AnimatedSwitches.Add(sw);
        //     }
        // }
        //
        // private void InitAnimations()
        // {
        //     InitBoomAnimations();
        //     InitAnimDefs();
        // }
        //
        // private void InitAnimDefs()
        // {
        //     foreach (var animTexture in m_resources.Animdefs.AnimatedTextures)
        //     {
        //         if (animTexture.Components.Count == 0)
        //             continue;
        //
        //         foreach (var component in animTexture.Components)
        //             component.TextureIndex = GetTexture(component.Texture, ResourceNamespace.Global).Index;
        //
        //         Animation animation = new Animation(animTexture, animTexture.Components[0].TextureIndex);
        //         m_animations.Add(animation);
        //         CreateComponentAnimations(animation);
        //     }
        // }
        //
        // private void InitBoomAnimations()
        // {
        //     foreach (var animTexture in m_resources.BoomAnimated.AnimatedTextures)
        //     {
        //         ResourceNamespace resNamespace = animTexture.IsTexture ? ResourceNamespace.Textures : ResourceNamespace.Flats;
        //         int startIndex = GetTexture(animTexture.StartTexture, resNamespace).Index;
        //         if (startIndex == Constants.NoTextureIndex)
        //             continue;
        //         int endIndex = GetTexture(animTexture.EndTexture, resNamespace).Index;
        //         if (endIndex == Constants.NoTextureIndex)
        //             continue;
        //
        //         if (endIndex <= startIndex)
        //             continue;
        //
        //         Animation animation = new Animation(new AnimatedTexture(GetTexture(startIndex).Name.ToString(), false, resNamespace), startIndex);
        //         m_animations.Add(animation);
        //
        //         for (int i = startIndex; i <= endIndex; i++)
        //         {
        //             TextureNew texture = GetTexture(i);
        //             var component = new AnimatedTextureComponent(texture.Name,
        //                 animTexture.Tics, animTexture.Tics, textureIndex: i);
        //             animation.AnimatedTexture.Components.Add(component);
        //         }
        //
        //         CreateComponentAnimations(animation);
        //     }
        // }
        // private void CreateComponentAnimations(Animation animation)
        // {
        //     for (int i = 1; i < animation.AnimatedTexture.Components.Count; i++)
        //     {
        //         int nextAnimIndex = animation.AnimatedTexture.Components[i].TextureIndex;
        //         Animation nextAnim = new Animation(new AnimatedTexture(GetTexture(nextAnimIndex).Name.ToString(), false, 
        //             animation.AnimatedTexture.Namespace), nextAnimIndex);
        //         m_animations.Add(nextAnim);
        //
        //         for (int j = 0; j < animation.AnimatedTexture.Components.Count; j++)
        //         {
        //             int index = (i + j + 1) % animation.AnimatedTexture.Components.Count;
        //             var component = animation.AnimatedTexture.Components[index];
        //             nextAnim.AnimatedTexture.Components.Add(component);
        //         }
        //     }
        // }
        //
        // private void InitTextureArrays(List<TextureDefinition> textures, List<Entry> flatEntries)
        // {
        //     for (int i = 0; i < m_translations.Count; i++)
        //         m_translations[i] = i;
        //
        //     m_textures[Constants.NoTextureIndex] = new TextureNew(Constants.NoTexture, ResourceNamespace.Textures, Constants.NoTextureIndex);
        //
        //     int index = Constants.NoTextureIndex + 1;
        //     foreach (TextureDefinition texture in textures)
        //     {
        //         m_textures[index] = new Texture(texture.Name, texture.Namespace, index);
        //         index++;
        //     }
        //
        //     // Load AASHITTY for information purposes - FloorRaiseByTexture needs it to emulate vanilla bug
        //     if (m_textures.Count > 1)
        //     {
        //         TextureNew shitty = m_textures[1];
        //         shitty.Image = m_imageRetriever.GetOnly(shitty.Name, shitty.Namespace);
        //     }
        //
        //     // TODO: When ZDoom's Textures lump becomes a thing, this will need updating.
        //     foreach (Entry flat in flatEntries)
        //     {
        //         m_textures[index] = new TextureNew(flat.Path.Name, ResourceNamespace.Flats, index);
        //
        //         // TODO fix with MapInfo when implemented
        //         if (flat.Path.Name.Equals(Constants.SkyTexture, StringComparison.OrdinalIgnoreCase))
        //             m_skyIndex = index;
        //
        //         index++;
        //     }
        // }
        //
        // private void LoadTextureImage(int textureIndex)
        // {
        //     var texture = m_textures[textureIndex];
        //     if (texture.Image == null)
        //         texture.Image = m_imageRetriever.GetOnly(texture.Name, texture.Namespace);
        // }
    }
}
