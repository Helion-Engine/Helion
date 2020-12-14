using Helion.ResourcesNew.Sprites;
using Helion.ResourcesNew.Textures;

namespace Helion.ResourcesNew.Definitions.Decorate
{
    public class DecorateManager
    {
        private readonly Resources m_resources;
        private readonly TextureManager m_textures;
        private readonly SpriteManager m_sprites;

        public DecorateManager(Resources resources, TextureManager textures, SpriteManager sprites)
        {
            m_resources = resources;
            m_textures = textures;
            m_sprites = sprites;
        }
    }
}
