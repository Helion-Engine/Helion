using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains sidedef information.
    /// </summary>
    public class SidedefsEntry : Entry
    {
        public SidedefsEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.Sidedefs;
    }
}
