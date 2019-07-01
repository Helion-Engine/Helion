using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry for a GL potentially visible set.
    /// </summary>
    public class GLPvsEntry : TextEntry
    {
        public GLPvsEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }
        
        public override ResourceType GetResourceType() => ResourceType.GLPVS;
    }
}