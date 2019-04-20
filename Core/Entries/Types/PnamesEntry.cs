using Helion.Resources;
using Helion.Resources.Definitions;
using Helion.Util;
using NLog;

namespace Helion.Entries.Types
{
    /// <summary>
    /// An entry that contains pnames information.
    /// </summary>
    public class PnamesEntry : Entry
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The pnames data.
        /// </summary>
        /// <remarks>
        /// If the entry was corrupt, it will be an empty set of data.
        /// </remarks>
        public Pnames Pnames { get; } = new Pnames();

        public PnamesEntry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace) :
            base(id, path, data, resourceNamespace)
        {
            Optional<Pnames> pnames = Pnames.From(data);
            if (pnames)
                Pnames = pnames.Value;
            else
            {
                log.Warn($"Corrupt Pnames at: {Path}");
                Corrupt = true;
            }
        }

        public override ResourceType GetResourceType() => ResourceType.Pnames;
    }
}
