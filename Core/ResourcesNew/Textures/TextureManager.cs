using Helion.ResourcesNew.Definitions.Textures;

namespace Helion.ResourcesNew.Textures
{
    public class TextureManager
    {
        private readonly Resources m_resources;
        private readonly TextureDefinitionManager m_textureDefinitions;

        public TextureManager(Resources resources, TextureDefinitionManager textureDefinitions)
        {
            m_resources = resources;
            m_textureDefinitions = textureDefinitions;
        }
    }
}
