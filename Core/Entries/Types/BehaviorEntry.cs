using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains behavior information.
    /// </summary>
    public class BehaviorEntry : Entry
    {
        public BehaviorEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.Behavior;
    }
}
