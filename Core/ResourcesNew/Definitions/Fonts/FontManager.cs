using Helion.ResourcesNew.Textures;

namespace Helion.ResourcesNew.Definitions.Fonts
{
    public class FontManager
    {
        private readonly Resources m_resources;
        private readonly TextureManager m_textures;

        public FontManager(Resources resources, TextureManager textures)
        {
            m_resources = resources;
            m_textures = textures;
        }
    }
}
