using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains things information.
    /// </summary>
    public class ThingsEntry : Entry
    {
        public ThingsEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.Things;
    }
}
