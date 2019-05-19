using Helion.Graphics.Palette;
using Helion.Resources;
using Helion.Util;
using NLog;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains palette information.
    /// </summary>
    public class PaletteEntry : Entry
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The underlying palette.
        /// </summary>
        /// <remarks>
        /// If the palette data is corrupt then this will be the default 
        /// palette. It is necessary to check if this entry was marked as
        /// corrupt or not.
        /// </remarks>
        public Palette Palette { get; } = Palettes.GetDefaultPalette();

        public PaletteEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
            Palette? palette = Palette.From(data);
            if (palette != null)
                Palette = palette;
            else
            {
                log.Warn("Corrupt palette at {0}", path);
                Corrupt = true;
            }
        }

        public override ResourceType GetResourceType() => ResourceType.Palette;
    }
}
