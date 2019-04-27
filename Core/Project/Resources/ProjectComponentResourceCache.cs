using Helion.Entries;
using Helion.Entries.Types;
using Helion.Resources;
using Helion.Resources.Definitions;
using Helion.Util;
using System.Collections.Generic;

namespace Helion.Project.Resources
{
    /// <summary>
    /// A cache of all the entries for a project component's archive.
    /// </summary>
    public class ProjectComponentResourceCache
    {
        /// <summary>
        /// All the resource definitions that alter the engine in some way 
        /// (such as texture definitions, decorate, etc).
        /// </summary>
        public DefinitionEntries DefinitionEntries = new DefinitionEntries();

        /// <summary>
        /// A list of all the most recent images by name only. If multiple
        /// entries exist with the same name, only the one later in the archive
        /// is available in this dictionary.
        /// </summary>
        public Dictionary<UpperString, ImageEntry> Images = new Dictionary<UpperString, ImageEntry>();

        /// <summary>
        /// A list of all the most recent palette images by name only. If 
        /// multiple entries exist with the same name, only the one later in 
        /// the archive is available in this dictionary.
        /// </summary>
        public Dictionary<UpperString, PaletteImageEntry> PaletteImages = new Dictionary<UpperString, PaletteImageEntry>();

        /// <summary>
        /// A list of all the most recent TTFs by name only. If multiple
        /// entries exist with the same name, only the one later in the archive
        /// is available in this dictionary.
        /// </summary>
        public Dictionary<UpperString, TrueTypeFontEntry> TrueTypeFonts = new Dictionary<UpperString, TrueTypeFontEntry>();

        private ResourceTracker<Entry> entryTracker = new ResourceTracker<Entry>();

        /// <summary>
        /// Tracks an entry. Handles to it will be cached in this data 
        /// structure for easier lookups later. Corrupt entries are not
        /// tracked.
        /// </summary>
        /// <remarks>
        /// Just because it is tracked doesn't mean it won't be replaced later 
        /// on by something with the same name.
        /// </remarks>
        /// <param name="entry">The entry to track.</param>
        public void TrackEntry(Entry entry)
        {
            if (entry.Corrupt)
                return;

            entryTracker.AddOrOverwrite(entry.Path.Name, entry.Namespace, entry);

            switch (entry)
            {
            case ImageEntry imageEntry:
                Images[entry.Path.Name] = imageEntry;
                break;
            case PaletteImageEntry paletteImageEntry:
                PaletteImages[entry.Path.Name] = paletteImageEntry;
                break;
            case PnamesEntry pnamesEntry:
                DefinitionEntries.Pnames = pnamesEntry.Pnames;
                break;
            case TextureXEntry textureXEntry:
                DefinitionEntries.TextureXList.Add(textureXEntry.TextureX);
                break;
            case TrueTypeFontEntry ttfEntry:
                TrueTypeFonts[entry.Path.Name] = ttfEntry;
                break;
            }
        }

        /// <summary>
        /// Looks up an entry by name. The most recent entry in the entire
        /// archive with the name is loaded.
        /// </summary>
        /// <param name="name">The name to look up.</param>
        /// <returns>The entry if it exists or an empty value if no entry had
        /// that name.</returns>
        public Optional<Entry> FindEntry(UpperString name)
        {
            return entryTracker.GetWithAny(name, ResourceNamespace.Global);
        }

        /// <summary>
        /// Similar to <see cref="FindEntry(UpperString)"/>, this funds the
        /// entry but also will attempt to return the type. If the name matches
        /// but the type is wrong, an empty value is returned.
        /// </summary>
        /// <typeparam name="T">The type to get.</typeparam>
        /// <param name="name">The name to get.</param>
        /// <returns>The entry of the type provided with the name, or an empty
        /// value if both conditions are not met.</returns>
        public Optional<T> FindEntryAs<T>(UpperString name)
        {
            Optional<Entry> entry = entryTracker.GetWithAny(name, ResourceNamespace.Global);
            return (entry && entry.Value is T entryType) ? entryType : Optional<T>.Empty();
        }
    }
}
