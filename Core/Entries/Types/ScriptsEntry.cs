using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains script information.
    /// </summary>
    public class ScriptsEntry : TextEntry
    {
        public ScriptsEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
        }

        public override ResourceType GetResourceType() => ResourceType.Scripts;
    }
}
