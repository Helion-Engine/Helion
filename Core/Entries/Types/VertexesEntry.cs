using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains vertex information.
    /// </summary>
    /// <remarks>
    /// The spelling is intentional (even though it should be VerticesEntry) so
    /// users know that it's not some other kind of vertex entry.
    /// </remarks>
    public class VertexesEntry : Entry
    {
        public VertexesEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.Vertexes;
    }
}
