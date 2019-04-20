using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains decorate information.
    /// </summary>
    public class DecorateEntry : Entry
    {
        public DecorateEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.Decorate;
    }
}
