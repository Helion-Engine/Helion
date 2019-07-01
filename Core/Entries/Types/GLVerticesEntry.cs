using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry for GL vertices.
    /// </summary>
    public class GLVerticesEntry : TextEntry
    {
        public GLVerticesEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }
        
        public override ResourceType GetResourceType() => ResourceType.GLVertices;
    }
}