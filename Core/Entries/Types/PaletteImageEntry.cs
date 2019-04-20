using Helion.Graphics.Palette;
using Helion.Resources;
using Helion.Util;
using NLog;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains palette image information.
    /// </summary>
    public class PaletteImageEntry : Entry
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The palette image that makes up this entry.
        /// </summary>
        /// <remarks>
        /// If this was failed to be generated due to corrupt data, it will
        /// contain a default image. This means the image is always safe to
        /// utilize but its corruption status should be checked to make sure
        /// its correct to use.
        /// </remarks>
        public PaletteImage PaletteImage { get; private set; } = new PaletteImage(1, 1);

        public PaletteImageEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
            if (PaletteReaders.LikelyFlat(data))
                HandleFlat();
            else
                HandleColumn();
        }

        private void HandleFlat()
        {
            Optional<PaletteImage> image = PaletteReaders.ReadFlat(Data, Namespace);
            if (image)
                PaletteImage = image.Value;
            else
            {
                log.Warn("Corrupt flat palette image at {0}", Path);
                Corrupt = true;
            }
        }

        private void HandleColumn()
        {
            Optional<PaletteImage> image = PaletteReaders.ReadColumn(Data, Namespace);
            if (image)
                PaletteImage = image.Value;
            else
            {
                log.Warn("Corrupt column palette image at {0}", Path);
                Corrupt = true;
            }
        }

        public override ResourceType GetResourceType() => ResourceType.PaletteImage;
    }
}
