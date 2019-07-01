using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry for a GL map marker.
    /// </summary>
    public class GLMapMarkerEntry : TextEntry
    {
        public GLMapMarkerEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }
        
        public override ResourceType GetResourceType() => ResourceType.GLMapMarker;
    }
}