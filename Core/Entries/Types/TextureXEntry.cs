using Helion.Resources;
using Helion.Resources.Definitions.Texture;
using NLog;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains texture1/2/3 (...etc) information.
    /// </summary>
    public class TextureXEntry : Entry
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The texture1/2/3 information for this entry.
        /// </summary>
        /// <remarks>
        /// If the data is corrupt, this will be empty. It is important to 
        /// check for corruption before using.
        /// </remarks>
        public TextureX TextureX = new TextureX();

        public TextureXEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
            TextureX? textureX = TextureX.From(data);
            if (textureX != null)
                TextureX = textureX;
            else
            {
                log.Warn("TextureX is corrupt at {0}", path);
                Corrupt = true;
            }
        }

        public override ResourceType GetResourceType() => ResourceType.TextureX;
    }
}
