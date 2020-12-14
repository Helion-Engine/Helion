using System.Collections.Generic;
using Helion.ResourcesNew.Textures;
using Helion.Util;

namespace Helion.ResourcesNew.Sprites
{
    /// <summary>
    /// Manages creation of sprites in a lazy way.
    /// </summary>
    public class SpriteManager
    {
        public readonly Sprite MissingSprite;
        private readonly Resources m_resources;
        private readonly TextureManager m_textures;
        private readonly Dictionary<CIString, Sprite> m_sprites = new();

        public SpriteManager(Resources resources, TextureManager textures)
        {
            MissingSprite = new("", textures.MissingTexture);
            m_resources = resources;
            m_textures = textures;
        }

        /// <summary>
        /// Gets the sprite from the five letter base name.
        /// </summary>
        /// <param name="name">The sprite base, five letters long only.</param>
        public Sprite this[CIString name] => GetSprite(name);

        private Sprite GetSprite(CIString name)
        {
            if (name.Length != 5)
                return MissingSprite;

            if (m_sprites.TryGetValue(name, out Sprite? sprite))
                return sprite;

            // TODO: Try and find/create resource if missing.

            // If we can't find/create one, substitute the missing one.
            m_sprites[name] = MissingSprite;
            return MissingSprite;
        }
    }
}
