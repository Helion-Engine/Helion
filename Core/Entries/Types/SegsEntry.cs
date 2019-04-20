using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains segment information.
    /// </summary>
    public class SegsEntry : Entry
    {
        public SegsEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.Segs;
    }
}
