using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that could not be classified.
    /// </summary>
    public class UnknownEntry : Entry
    {
        public UnknownEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.Unknown;
    }
}
