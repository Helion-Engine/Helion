using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains blockmap information.
    /// </summary>
    public class BlockmapEntry : Entry
    {
        public BlockmapEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.Blockmap;
    }
}
