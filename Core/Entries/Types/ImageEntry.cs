using Helion.Graphics;
using Helion.Resources;
using Helion.Util;
using NLog;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains image information.
    /// </summary>
    public class ImageEntry : Entry
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The image that makes up this entry.
        /// </summary>
        /// <remarks>
        /// If this was failed to be generated due to corrupt data, it will
        /// contain a default image. This means the image is always safe to
        /// utilize but its corruption status should be checked to make sure
        /// its correct to use.
        /// </remarks>
        public Image Image { get; } = new Image(1, 1);

        public ImageEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
            Image? image = ImageReader.Read(data, resourceNamespace);
            if (image != null)
                Image = image;
            else
            {
                log.Warn("Unable to read image at {0}", path);
                Corrupt = true;
            }
        }

        public override ResourceType GetResourceType() => ResourceType.Image;
    }
}
