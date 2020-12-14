using System.Collections.Generic;
using Helion.Graphics;
using Helion.ResourcesNew.Definitions.Textures;

namespace Helion.ResourcesNew.Textures
{
    public class TextureManager
    {
        public readonly Texture MissingTexture;
        private readonly Resources m_resources;
        private readonly TextureDefinitionManager m_textureDefinitions;
        private readonly List<Texture> m_textures = new();

        public TextureManager(Resources resources, TextureDefinitionManager textureDefinitions)
        {
            m_resources = resources;
            m_textureDefinitions = textureDefinitions;

            MissingTexture = CreateMissingTexture();
            m_textures.Add(MissingTexture);
        }

        private static Texture CreateMissingTexture()
        {
            Image nullImage = ImageHelper.CreateNullImage();
            return new("", nullImage, Namespace.Global, 0, true);
        }
    }
}
