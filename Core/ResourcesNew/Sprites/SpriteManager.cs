using Helion.ResourcesNew.Textures;

namespace Helion.ResourcesNew.Sprites
{
    public class SpriteManager
    {
        private readonly Resources m_resources;
        private readonly TextureManager m_textures;

        public SpriteManager(Resources resources, TextureManager textures)
        {
            m_resources = resources;
            m_textures = textures;
        }
    }
}
