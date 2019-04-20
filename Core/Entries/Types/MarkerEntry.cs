using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry for a marker.
    /// </summary>
    public class MarkerEntry : Entry
    {
        public MarkerEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.Marker;
    }
}
