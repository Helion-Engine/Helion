using System.Collections.Generic;
using System.Linq;
using Helion.Resource.Textures;
using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resource.Sprites
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

            Sprite? newSprite = TryCreateSprite(name);
            if (newSprite != null)
            {
                m_sprites[name] = newSprite;
                return newSprite;
            }

            // If we can't find/create one, substitute the missing one.
            m_sprites[name] = MissingSprite;
            return MissingSprite;
        }

        private Sprite? TryCreateSprite(CIString name)
        {
            Precondition(name.Length == 5, "Sprite name needs to be exactly 5 characters");

            Texture frame0 = m_textures.Get(name + "0", Namespace.Sprites);
            Texture frame1 = m_textures.Get(name + "1", Namespace.Sprites);
            Texture frame2 = m_textures.Get(name + "2", Namespace.Sprites);
            Texture frame3 = m_textures.Get(name + "3", Namespace.Sprites);
            Texture frame4 = m_textures.Get(name + "4", Namespace.Sprites);
            Texture frame5 = m_textures.Get(name + "5", Namespace.Sprites);
            Texture frame6 = m_textures.Get(name + "6", Namespace.Sprites);
            Texture frame7 = m_textures.Get(name + "7", Namespace.Sprites);
            Texture frame8 = m_textures.Get(name + "8", Namespace.Sprites);
            Texture frame28 = m_textures.Get(name + "2" + name[4] + "8", Namespace.Sprites);
            Texture frame37 = m_textures.Get(name + "3" + name[4] + "7", Namespace.Sprites);
            Texture frame46 = m_textures.Get(name + "4" + name[4] + "6", Namespace.Sprites);

            // if (frame1.IsMissing && frame28.IsMissing && frame37.IsMissing && frame46.IsMissing && frame5.IsMissing)
            if (AreAllMissing(frame1, frame28, frame37, frame46, frame5))
                return new(name, frame1, frame28, frame37, frame46, frame5);
            if (AreAllMissing(frame1, frame2, frame3, frame4, frame5, frame6, frame7, frame8))
                return new(name, frame1, frame2, frame3, frame4, frame5, frame6, frame7, frame8);
            return frame0.IsMissing ? new Sprite(name, frame0) : null;

            static bool AreAllMissing(params Texture[] textures) => textures.All(texture => texture.IsMissing);
        }
    }
}
