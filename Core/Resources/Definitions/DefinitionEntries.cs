using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Definitions.Texture;
using Helion.Util;

namespace Helion.Resources.Definitions
{
    /// <summary>
    /// A collection of definition files that make up new stuff.
    /// </summary>
    public class DefinitionEntries
    {
        public Pnames? Pnames;
        private List<TextureXImage> TextureXList = new List<TextureXImage>();

        public void AddTextureX(TextureX textureX)
        {
            TextureXList.AddRange(textureX.Definitions);
        }

        public TextureXImage? GetTextureXImage(CiString name)
        {
            return TextureXList.FirstOrDefault(x => x.Name == name);
        }
    }
}
