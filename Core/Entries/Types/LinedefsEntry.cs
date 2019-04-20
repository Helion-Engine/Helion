using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains linedef information.
    /// </summary>
    public class LinedefsEntry : Entry
    {
        public LinedefsEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.Linedefs;
    }
}
