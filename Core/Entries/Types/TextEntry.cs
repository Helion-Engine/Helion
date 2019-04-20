using System.Text;
using Helion.Resources;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains text.
    /// </summary>
    public class TextEntry : Entry
    {
        /// <summary>
        /// The UTF-8 text that this entry was made up from.
        /// </summary>
        public string Text { get; }

        public TextEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
            Text = Encoding.UTF8.GetString(data);
        }

        public override ResourceType GetResourceType() => ResourceType.Text;
    }
}
