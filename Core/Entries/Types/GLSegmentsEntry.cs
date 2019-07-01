using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry for GL segments.
    /// </summary>
    public class GLSegmentsEntry : TextEntry
    {
        public GLSegmentsEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }
        
        public override ResourceType GetResourceType() => ResourceType.GLSegments;
    }
}