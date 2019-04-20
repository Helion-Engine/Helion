using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains reject table information.
    /// </summary>
    public class RejectEntry : Entry
    {
        public RejectEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.Reject;
    }
}
