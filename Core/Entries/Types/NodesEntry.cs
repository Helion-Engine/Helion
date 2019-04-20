using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains map node information.
    /// </summary>
    public class NodesEntry : Entry
    {
        public NodesEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.Nodes;
    }
}
