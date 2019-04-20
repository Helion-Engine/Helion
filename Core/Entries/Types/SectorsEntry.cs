using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains sector information.
    /// </summary>
    public class SectorsEntry : Entry
    {
        public SectorsEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.Sectors;
    }
}
