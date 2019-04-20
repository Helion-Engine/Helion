using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains raw TTF data.
    /// </summary>
    public class TrueTypeFontEntry : Entry
    {
        public TrueTypeFontEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.TrueTypeFont;
    }
}
