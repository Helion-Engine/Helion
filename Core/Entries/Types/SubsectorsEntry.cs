using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains subsector information.
    /// </summary>
    public class SubsectorsEntry : Entry
    {
        public SubsectorsEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.Subsectors;
    }
}
